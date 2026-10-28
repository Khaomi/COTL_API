using HarmonyLib;
using Lamb.UI.BuildMenu;
using UnityEngine;

namespace COTL_API.CustomStructures;

[HarmonyPatch]
public partial class CustomStructureManager
{
    [HarmonyPatch(typeof(StructureBrain), nameof(StructureBrain.CreateBrain))]
    [HarmonyPrefix]
    private static bool StructureBrain_CreateBrain(ref StructureBrain __result, StructuresData data)
    {
        StructureBrain_ApplyConfigToData(data);
        if (!CustomStructureList.TryGetValue(data.Type, out var value)) return true;
        var t = value.GetType();
        var structureBrain = Activator.CreateInstance(t) as StructureBrain;

        StructureBrain.ApplyConfigToData(data);
        structureBrain?.Init(data);
        StructureBrain.TryAddBrain(data.ID, structureBrain);
        StructureManager.StructuresAtLocation(data.Location).Add(structureBrain);
        __result = structureBrain!;
        return false;
    }

    [HarmonyPatch(typeof(StructureBrain), nameof(StructureBrain.ApplyConfigToData))]
    [HarmonyPrefix]
    private static void StructureBrain_ApplyConfigToData(StructuresData data)
    {
        var prefabPath = data.PrefabPath;
        if (prefabPath == null || !prefabPath.Contains("CustomBuildingPrefab_")) return;

        if (CustomStructureList.TryGetValue(data.Type, out var current) &&
            (current.PrefabPath == prefabPath || $"Assets/{current.PrefabPath}.prefab" == prefabPath)) return;

        var structure = GetStructureByPrefabName(prefabPath);
        if (structure == null) return;

        LogWarning($"{prefabPath} was saved as {data.Type}, restoring its current id {structure.StructureType}");
        data.Type = structure.StructureType;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetInfoByType))]
    [HarmonyPrefix]
    private static bool StructuresData_GetInfoByType(ref StructuresData __result, StructureBrain.TYPES Type,
        int variantIndex)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.StructuresData;

        __result.PrefabPath ??= CustomStructureList[Type].PrefabPath;

        __result.Type = Type;
        __result.VariantIndex = variantIndex;
        __result.IsUpgrade = StructuresData.IsUpgradeStructure(__result.Type);
        __result.UpgradeFromType = StructuresData.GetUpgradePrerequisite(__result.Type);

        return false;
    }

    [HarmonyPatch(typeof(FollowerCategory), nameof(FollowerCategory.GetStructuresForCategory))]
    [HarmonyPostfix]
    private static void FollowerCategory_GetStructuresForCategory(ref List<StructureBrain.TYPES> __result,
        FollowerCategory.Category category)
    {
        __result.AddRange(from structure in CustomStructureList.Values
            where structure.Category == category
            select structure.StructureType);
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetUnlocked))]
    [HarmonyPrefix]
    private static void StructuresData_GetUnlocked(StructureBrain.TYPES Types)
    {
        if (!CustomStructureList.ContainsKey(Types)) return;
        if (!DataManager.Instance.UnlockedStructures.Contains(Types))
            DataManager.Instance.UnlockedStructures.Add(Types);
        if (!DataManager.Instance.RevealedStructures.Contains(Types))
            DataManager.Instance.RevealedStructures.Add(Types);
    }

    [HarmonyPatch(typeof(TypeAndPlacementObjects), nameof(TypeAndPlacementObjects.GetByType))]
    [HarmonyPrefix]
    private static bool TypeAndPlacementObjects_GetByType(ref TypeAndPlacementObject __result,
        StructureBrain.TYPES Type)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.GetTypeAndPlacementObject();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.BuildDurationGameMinutes))]
    [HarmonyPrefix]
    private static bool StructuresData_BuildDurationGameMinutes(ref int __result, StructureBrain.TYPES Type)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.BuildDurationMinutes;
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetCost))]
    [HarmonyPrefix]
    private static bool StructuresData_GetCost(ref List<StructuresData.ItemCost> __result, StructureBrain.TYPES Type)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.Cost;
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetLocalizedNameStatic), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_GetLocalizedNameStatic(StructureBrain.TYPES Type, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.GetLocalizedNameStatic();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.LocalizedName), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_LocalizedName(StructureBrain.TYPES Type, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.LocalizedName();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.LocalizedDescription), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_LocalizedDescription(StructureBrain.TYPES Type, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.LocalizedDescription();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.LocalizedPros), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_LocalizedPros(StructureBrain.TYPES Type, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.LocalizedPros();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.LocalizedCons), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_LocalizedCons(StructureBrain.TYPES Type, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.LocalizedCons();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetLocalizedName), [])]
    [HarmonyPrefix]
    private static bool StructuresData_GetLocalizedName(StructuresData __instance, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(__instance.Type, out var value)) return true;
        __result = value.GetLocalizedName();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetLocalizedName), typeof(bool), typeof(bool),
        typeof(bool))]
    [HarmonyPrefix]
    private static bool StructuresData_GetLocalizedName(StructuresData __instance, bool plural, bool withArticle,
        bool definite, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(__instance.Type, out var value)) return true;
        __result = value.GetLocalizedName(plural, withArticle, definite);
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetLocalizedDescription), [])]
    [HarmonyPrefix]
    private static bool StructuresData_GetLocalizedDescription(StructuresData __instance, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(__instance.Type, out var value)) return true;
        __result = value.GetLocalizedDescription();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetLocalizedLore), [])]
    [HarmonyPrefix]
    private static bool StructuresData_GetLocalizedLore(StructuresData __instance, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(__instance.Type, out var value)) return true;
        __result = value.GetLocalizedLore();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetResearchCost), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_GetResearchCost(StructureBrain.TYPES Type, ref int __result)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.GetResearchCost();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.RequiresTempleToBuild), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_RequiresTempleToBuild(StructureBrain.TYPES type, ref bool __result)
    {
        if (!CustomStructureList.TryGetValue(type, out var value)) return true;
        __result = value.RequiresTempleToBuild();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetBuildOnlyOne), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_GetBuildOnlyOne(StructureBrain.TYPES Type, ref bool __result)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.GetBuildOnlyOne();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.GetBuildSfx), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_GetBuildSfx(StructureBrain.TYPES Type, ref string __result)
    {
        if (!CustomStructureList.TryGetValue(Type, out var value)) return true;
        __result = value.GetBuildSfx();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.HiddenUntilUnlocked), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_HiddenUntilUnlocked(StructureBrain.TYPES structure, ref bool __result)
    {
        if (!CustomStructureList.TryGetValue(structure, out var value)) return true;
        __result = value.HiddenUntilUnlocked();
        return false;
    }

    [HarmonyPatch(typeof(StructuresData), nameof(StructuresData.CanBeFlipped), typeof(StructureBrain.TYPES))]
    [HarmonyPrefix]
    private static bool StructuresData_CanBeFlipped(StructureBrain.TYPES type, ref bool __result)
    {
        if (!CustomStructureList.TryGetValue(type, out var value)) return true;
        __result = value.CanBeFlipped();
        return false;
    }

    [HarmonyPatch(typeof(Structure), nameof(Structure.BrainAssigned))]
    [HarmonyPostfix]
    private static void Structure_BrainAssigned(Structure __instance)
    {
        if (!CustomStructureList.TryGetValue(__instance.Type, out var value)) return;
        if (value.Interaction == null) return;

        LogDebug("adding structure interaction " +
                 CustomStructureList[__instance.Type].Interaction);
        var parent = __instance.GetComponentInParent<Transform>();
        parent.gameObject.AddComponent(CustomStructureList[__instance.Type].Interaction);
    }
}