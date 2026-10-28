using System.Reflection;
using HarmonyLib;

namespace COTL_API.Saves;

public class ModdedSaveData<T> : BaseModdedSaveData where T : class, new()
{
    private static bool? _messagePackCompatible;

    private readonly COTLDataReadWriter<T> _dataReadWriter = new();
    private Action<T, string, bool, bool>? _jsonWriter;

    public ModdedSaveData(string guid)
    {
        GUID = guid;

        _dataReadWriter.OnReadCompleted += delegate(T saveData)
        {
            Data = saveData;
            IsLoaded = true;
            OnLoadComplete?.Invoke();
        };

        _dataReadWriter.OnCreateDefault += delegate
        {
            CreateDefault();
            OnLoadComplete?.Invoke();
            IsLoaded = true;
            OnSaveCompleted?.Invoke();
        };

        _dataReadWriter.OnWriteCompleted += delegate { OnSaveCompleted?.Invoke(); };
        _dataReadWriter.OnWriteError += delegate(MMReadWriteError error) { OnSaveError?.Invoke(error); };
    }

    public override int SAVE_SLOT { get; protected set; } = 5;
    public override ModdedSaveLoadOrder LoadOrder { get; set; } = ModdedSaveLoadOrder.LOAD_AFTER_SAVE_START;

    public sealed override string GUID { get; protected set; }
    public override bool IsLoaded { get; protected set; }

    public T? Data { get; private set; }

    public override void CreateDefault()
    {
        Data = new T();
    }

    public override void Save(bool encrypt = true, bool backup = true)
    {
        if (LoadOrder != ModdedSaveLoadOrder.LOAD_AS_SOON_AS_POSSIBLE &&
            (!DataManager.Instance.AllowSaving || CheatConsole.IN_DEMO))
            return;

        if (Data != null)
            Write(Data,
                MakeSaveSlot(LoadOrder == ModdedSaveLoadOrder.LOAD_AS_SOON_AS_POSSIBLE ? null : SAVE_SLOT), encrypt,
                backup);
    }

    public override void Saving()
    {
        if (Data != null)
            Write(Data,
                MakeSaveSlot(LoadOrder == ModdedSaveLoadOrder.LOAD_AS_SOON_AS_POSSIBLE ? null : SAVE_SLOT));
    }

    private void Write(T data, string filename, bool encrypt = true, bool backup = true)
    {
        var jsonWriter = IsMessagePackCompatible() ? null : GetJsonWriter();
        if (jsonWriter != null)
            jsonWriter(data, filename, encrypt, backup);
        else
            _dataReadWriter.Write(data, filename, encrypt, backup);
    }

    //using reflection because of outdated gamelibs, which is not updated in the nuget server
    private static bool IsMessagePackCompatible()
    {
        if (_messagePackCompatible != null) return _messagePackCompatible.Value;

        var optionsType = AccessTools.TypeByName("MPSerialization");
        var options = optionsType == null ? null : AccessTools.Field(optionsType, "options")?.GetValue(null);
        var serialize = AccessTools.TypeByName("MessagePack.MessagePackSerializer")?.GetMethods()
            .FirstOrDefault(method => method is { Name: "Serialize", IsGenericMethodDefinition: true } &&
                                      method.ReturnType == typeof(byte[]) && method.GetParameters().Length == 3);
        if (options == null || serialize == null)
        {
            _messagePackCompatible = true;
            return true;
        }

        try
        {
            serialize.MakeGenericMethod(typeof(T)).Invoke(null, [new T(), options, CancellationToken.None]);
            _messagePackCompatible = true;
        }
        catch (TargetInvocationException)
        {
            LogWarning($"{typeof(T).FullName} can't be saved as MessagePack, saving it as JSON instead");
            _messagePackCompatible = false;
        }

        return _messagePackCompatible.Value;
    }

    //using reflection because of outdated gamelibs, which is not updated in the nuget server
    private Action<T, string, bool, bool>? GetJsonWriter()
    {
        if (_jsonWriter != null) return _jsonWriter;

        var readWriter = AccessTools.Field(typeof(COTLDataReadWriter<T>), "_readWriter")?.GetValue(_dataReadWriter);
        var writeJson = readWriter == null ? null : AccessTools.Method(readWriter.GetType(), "WriteJson");
        if (writeJson == null)
        {
            LogError($"Couldn't find the game's JSON writer for {typeof(T).FullName}");
            return null;
        }

        _jsonWriter = (Action<T, string, bool, bool>)Delegate.CreateDelegate(
            typeof(Action<T, string, bool, bool>), readWriter, writeJson);
        return _jsonWriter;
    }

    public override void Load(int? saveSlot = null)
    {
        if (LoadOrder != ModdedSaveLoadOrder.LOAD_AS_SOON_AS_POSSIBLE && CheatConsole.IN_DEMO)
            return;

        if (saveSlot != null)
            SAVE_SLOT = saveSlot.Value;

        _dataReadWriter.Read(MakeSaveSlot(LoadOrder == ModdedSaveLoadOrder.LOAD_AS_SOON_AS_POSSIBLE
            ? null
            : SAVE_SLOT));
    }

    public override bool SaveExist(int? saveSlot = null)
    {
        return _dataReadWriter.FileExists(MakeSaveSlot(saveSlot));
    }

    public override void DeleteSaveSlot(int? saveSlot = null)
    {
        _dataReadWriter.Delete(MakeSaveSlot(saveSlot));
        if (saveSlot != null)
            OnSaveSlotDeleted?.Invoke(saveSlot.Value);
    }

    public override void ResetSave(int? saveSlot = null, bool newGame = false)
    {
        if (saveSlot != null)
            SAVE_SLOT = saveSlot.Value;
        CreateDefault();
        if (!newGame)
            Save();
        IsLoaded = true;
    }

    public override string MakeSaveSlot(int? slot = null)
    {
        return slot != null ? $"{GUID}_{slot}.json" : $"{GUID}.json";
    }
}