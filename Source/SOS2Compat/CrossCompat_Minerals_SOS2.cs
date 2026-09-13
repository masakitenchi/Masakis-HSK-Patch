using System.Reflection;

namespace Core_SK_Patch;

[HarmonyPatch]
public class CrossCompat_Minerals_SOS2
{
    private static Action<Map> initRocks;

    [HarmonyPrepare]
    public static bool Prepare(MethodBase original)
    {
        if (ModLister.GetActiveModWithIdentifier("kentington.saveourship2", ignorePostfix: true) == null
            || ModLister.GetActiveModWithIdentifier("zacharyfoster.minerals", ignorePostfix: true) == null)
            return false;
        if (initRocks != null)
            return true;

        // Resolve optional integration once, without an assembly reference to Minerals.
        var type = AccessTools.TypeByName("Minerals.mapBuilder");
        var method = type == null ? null : AccessTools.Method(type, "initRocks", new[] { typeof(Map) });
        if (method == null || !method.IsStatic || method.ReturnType != typeof(void))
        {
            Log.Warning("SOS2Compat: Minerals initRocks(Map) is unavailable; skipping mineral conversion.");
            return false;
        }
        initRocks = (Action<Map>)Delegate.CreateDelegate(typeof(Action<Map>), method);
        return true;
    }

    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> TargetMethods()
    {
        yield return AccessTools.Method("SaveOurShip2.ShipInteriorMod2:PostGenerateShipDef");
        yield return AccessTools.Method("SaveOurShip2.GenStep_ValuableAsteroids:Generate");
    }

    [HarmonyPostfix]
    public static void Postfix(Map map)
    {
        // Convert only after generation, keeping custom crystal yields out of SOS2's ore sizing.
        bool updaterWasEnabled = map.regionAndRoomUpdater.Enabled;
        try
        {
            initRocks(map);
        }
        finally
        {
            map.regionAndRoomUpdater.Enabled = updaterWasEnabled;
        }
    }
}
