using Rimatomics;
using SaveOurShip2;

namespace Core_SK_Patch;

[HarmonyPatch(typeof(UniversalPipeMapComp), nameof(UniversalPipeMapComp.MapComponentTick))]
internal static class RimatomicsSOS2HeatBridgeMapTickPatch
{
    [HarmonyPrepare]
    private static bool Prepare()
    {
        return ModsConfig.IsActive("dubwise.rimatomics") &&
            ModsConfig.IsActive("kentington.saveourship2");
    }

    [HarmonyPostfix]
    private static void Postfix(UniversalPipeMapComp __instance)
    {
        RimatomicsSOS2HeatBridgeRuntime.Process(__instance);
    }
}

[HarmonyPatch(typeof(CompShipHeat), nameof(CompShipHeat.CompInspectStringExtra))]
internal static class RimatomicsSOS2HeatBridgeInspectPatch
{
    [HarmonyPrepare]
    private static bool Prepare()
    {
        return ModsConfig.IsActive("dubwise.rimatomics") &&
            ModsConfig.IsActive("kentington.saveourship2");
    }

    [HarmonyPostfix]
    private static void Postfix(CompShipHeat __instance, ref string __result)
    {
        RimatomicsSOS2HeatBridgeDef config = RimatomicsSOS2HeatBridgeRuntime.Config;
        if (config == null || !config.IsBridgeThing(__instance.parent.def))
            return;

        CompPipe coolingPipe = __instance.parent.GetComps<CompPipe>()
            .FirstOrDefault(pipe => pipe.mode == PipeType.Cooling);
        bool connected = __instance.myNet != null && coolingPipe?.net is CoolingNet &&
            (!config.spaceOnly || __instance.parent.Map?.IsSpace() == true);
        string bridgeStatus = connected
            ? "CoreSK_RimatomicsSOS2BridgeActive".Translate(
                (RimatomicsSOS2HeatBridgeRuntime.GetAvailableCoolingWatts(__instance.myNet) / 1000f).ToString("F1"))
            : "CoreSK_RimatomicsSOS2BridgeDisconnected".Translate();

        __result = __result.NullOrEmpty() ? bridgeStatus : __result + "\n" + bridgeStatus;
    }
}
