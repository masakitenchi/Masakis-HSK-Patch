using System.Reflection;
using Minerals;

namespace Core_SK_Patch;

[HarmonyPatch]
public class CrossCompat_Minerals_SOS2
{
    [HarmonyPrepare]
    public static bool Prepare(MethodBase original)
    {
        if (!ModsConfig.IsActive("kentington.saveourship2") || !ModsConfig.IsActive("zacharyfoster.minerals"))
        {
            return false;
        }
        return true;
    }

    [HarmonyTargetMethod]
    public static MethodInfo TargetMethod()
    {
        return AccessTools.Method("SaveOurShip2.ShipInteriorMod2:PostGenerateShipDef");
    }

    [HarmonyPrefix]
    public static bool Prefix(Map map, bool clearArea, List<IntVec3> shipArea, List<Thing> planters)
    {
        AccessTools.Method("Minerals.mapBuilder:initRocks").Invoke(obj: null, parameters: new object[] { map });
        return true;
    }
}
