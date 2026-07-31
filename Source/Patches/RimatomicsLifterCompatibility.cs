namespace Core_SK_Patch;

/// <summary>
/// Makes Rimatomics' NuclearWork available to lifters and repairs work
/// settings that were initialized before the XML compatibility patch existed.
/// </summary>
[HarmonyPatch]
public static class RimatomicsLifterCompatibility
{
    private const string RimatomicsPackageId = "dubwise.rimatomics";
    private const string LifterDefName = "Mech_Lifter";
    private const string NuclearWorkDefName = "NuclearWork";
    private const int DefaultPriority = 3;

    private static WorkTypeDef NuclearWork =>
        DefDatabase<WorkTypeDef>.GetNamedSilentFail(NuclearWorkDefName);

    private static bool IsApplicable(Pawn pawn, WorkTypeDef workType)
    {
        return ModsConfig.BiotechActive
            && ModsConfig.IsActive(RimatomicsPackageId)
            && pawn?.def?.defName == LifterDefName
            && workType?.defName == NuclearWorkDefName;
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetDisabledWorkTypes))]
    [HarmonyPostfix]
    private static void EnableNuclearWork(Pawn __instance, ref List<WorkTypeDef> __result)
    {
        WorkTypeDef workType = NuclearWork;
        if (IsApplicable(__instance, workType))
        {
            __result?.Remove(workType);
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.EnableAndInitialize))]
    [HarmonyPostfix]
    private static void InitializeNuclearWorkPriority(
        Pawn_WorkSettings __instance,
        Pawn ___pawn)
    {
        EnablePriorityIfNeeded(__instance, ___pawn);
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.ExposeData))]
    [HarmonyPostfix]
    private static void RepairLoadedNuclearWorkPriority(
        Pawn_WorkSettings __instance,
        Pawn ___pawn)
    {
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            EnablePriorityIfNeeded(__instance, ___pawn);
        }
    }

    private static void EnablePriorityIfNeeded(
        Pawn_WorkSettings workSettings,
        Pawn pawn)
    {
        if (workSettings?.Initialized != true)
        {
            return;
        }

        WorkTypeDef workType = NuclearWork;
        if (IsApplicable(pawn, workType)
            && workSettings.GetPriority(workType) == 0)
        {
            workSettings.SetPriority(workType, DefaultPriority);
        }
    }
}
