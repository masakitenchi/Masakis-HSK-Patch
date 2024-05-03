using HarmonyLib;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class NeverDieByLowHealth
{
    public static bool Prepare()
    {
        return Settings.NeverDieByLowHealth;
    }

    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.ShouldBeDeadFromLethalDamageThreshold))]
    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}
