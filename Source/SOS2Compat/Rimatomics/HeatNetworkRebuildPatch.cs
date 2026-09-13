using SaveOurShip2;

namespace Core_SK_Patch;

[HarmonyPatch(typeof(ShipMapComp), nameof(ShipMapComp.MapComponentUpdate))]
internal static class HeatNetworkRebuildPatch
{
    private static bool Prepare() =>
        ModLister.GetActiveModWithIdentifier("dubwise.rimatomics", ignorePostfix: true) != null;

    [HarmonyPrefix]
    private static void Prefix(ShipMapComp __instance, out object __state) => __state = __instance.cachedNets;

    [HarmonyPostfix]
    private static void Postfix(ShipMapComp __instance, object __state)
    {
        // SOS2 publishes a new list after assigning all CompShipHeat.myNet links.
        // Refresh on completion, not while those links are being rebuilt. This
        // is capacity-only: MapComponentTick remains the sole heat-transfer pass.
        if (!ReferenceEquals(__state, __instance.cachedNets))
            RimatomicsSOS2HeatBridgeRuntime.RefreshAfterHeatRebuild(__instance.map);
    }
}
