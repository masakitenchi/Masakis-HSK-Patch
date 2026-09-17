using System.Reflection;
using SaveOurShip2;

namespace Core_SK_Patch;

// Odyssey must read SOS2's breathable ship rooms as pressurized, including on asteroid maps.
[HarmonyPatch]
internal static class ShipLifeSupportVacuumPatch
{
    private static bool Prepare() => ModsConfig.OdysseyActive;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertyGetter(typeof(Room), nameof(Room.Vacuum));
        // Doorway averaging, room exchange and the oxygen overlay also read the raw value.
        yield return AccessTools.PropertyGetter(typeof(Room), "UnsanitizedVacuum");
    }

    private static void Postfix(Room __instance, ref float __result)
    {
        if (__result > 0f && HasShipLifeSupport(__instance))
            __result = 0f;
    }

    private static bool HasShipLifeSupport(Room room)
    {
        if (room == null || room.Dereferenced || room.IsDoorway || ShipInteriorMod2.MoveShipFlag)
            return false;
        Map map = room.Map;
        if (map == null || !map.Biome.inVacuum || !map.regionAndRoomUpdater.Enabled
            || ShipInteriorMod2.ExposedToOutside(room))
            return false;

        // A sealed ship room belongs to its hull's cache; do not scan every cell on each read.
        var ships = map.GetComponent<ShipMapComp>();
        if (ships == null || !ships.ShipsOnMap.TryGetValue(ships.ShipIndexOnVec(room.FirstRegion.AnyCell), out var ship))
            return false;
        foreach (var support in ship.LifeSupports)
        {
            var building = support?.parent;
            if (support?.active == true && building != null && building.Spawned && building.Map == map
                && building.TryGetComp<CompPowerTrader>()?.PowerOn == true
                && building.TryGetComp<CompFlickable>()?.SwitchIsOn == true)
                return true;
        }
        return false;
    }
}
