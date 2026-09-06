using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SaveOurShip2;
using UnityEngine;

namespace Core_SK_Patch;

// SOS2 reads RatioInNetworkRaw directly when saving sinks and rebuilding nets.
// Updating only ratioDirty leaves those reads stale until someone reads RatioInNetwork.
[HarmonyPatch]
internal static class HeatNetConsistency
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (string name in new[]
        {
            "AddHeat", "RemoveHeat", "AddDepletion", "RemoveDepletion", "Register", "DeRegister"
        })
            yield return AccessTools.Method(typeof(ShipHeatNet), name);
    }

    [HarmonyPostfix]
    private static void Postfix(ShipHeatNet __instance, ref bool ___ratioDirty)
    {
        // Do not read RatioInNetwork here: Register can run before any sink exists,
        // and the upstream getter also mutates capacity in its empty-net branch.
        __instance.RatioInNetworkRaw = __instance.StorageCapacityRaw > 0f
            ? Mathf.Clamp01(__instance.StorageUsed / __instance.StorageCapacityRaw)
            : 0f;
        // Depletion changes effective capacity even when StorageUsed is unchanged.
        ___ratioDirty = true;
    }
}
