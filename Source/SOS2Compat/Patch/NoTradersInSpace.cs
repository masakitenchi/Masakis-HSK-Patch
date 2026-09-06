using System.Reflection;
using SaveOurShip2;

namespace Core_SK_Patch;

// RimPact Skips All Other Conditions about if caravans can arrive at certain map.
// This Patch Fixes it only if RimPacts is active (SOS2 already handles most of the case)
[HarmonyPatch]
public static class TraderPatchForRimPacts
{
    [HarmonyPrepare]
    public static bool Prepare()
    {
        return AccessTools.Method("RimPacts.WorldComponent_RimPacts:TrySpawnTradeCaravanRobust") != null;
    }

    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), nameof(IncidentWorker_TraderCaravanArrival.TryExecuteWorker))]
    [HarmonyPrefix]
    public static bool SkipCaravanInSpace(ref bool __result, IncidentParms parms)
    {
        if (parms.target is Map map && map.IsSpace())
        {
            __result = false;
            return false; // 跳过原方法
        }
        return true;
    }
}
