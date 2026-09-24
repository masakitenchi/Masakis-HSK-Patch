using SaveOurShip2;

namespace Core_SK_Patch;

internal static class SalvageBayDropPods
{
    internal sealed class Arrival
    {
        internal Map Map;
        internal ActiveTransporterInfo Info;
        internal IntVec3 Cell;
    }

    [ThreadStatic] internal static Arrival Current;
    private static readonly System.Reflection.MethodInfo Reserved = AccessTools.Method(typeof(CompShipBay), "IsReservedForOtherShuttle");

    internal static bool Friendly(ActiveTransporterInfo info, Faction faction)
    {
        if (info?.innerContainer == null || info.innerContainer.Count == 0) return false;
        // An explicit sender owns the delivery; hostile salvage passengers are not raiders.
        if (faction != null) return !faction.HostileTo(Faction.OfPlayer);
        // Only unknown senders need a direct passenger check. Do not traverse inventories/corpses.
        return !info.innerContainer.OfType<Pawn>().Any(pawn => pawn.HostileTo(Faction.OfPlayer));
    }

    internal static bool TryCell(Map map, out IntVec3 result)
    {
        result = IntVec3.Invalid;
        if (map == null || !map.IsSpace() || ShipInteriorMod2.MoveShipFlag || Reserved == null) return false;
        var comp = map.GetComponent<ShipMapComp>();
        if (comp?.Bays == null) return false;
        // Match SOS2's final centre-cell routing while authorising Stratum roof passage.
        foreach (var bay in comp.Bays.OfType<CompShipBaySalvage>().OrderBy(b => b.parent.thingIDNumber))
        {
            if (!bay.parent.Spawned || bay.parent.Map != map || bay.parent.Faction != Faction.OfPlayer
                || bay.parent.Destroyed || bay.parent.IsBurning() || bay.parent.IsBrokenDown()
                || !(bay.parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? true)
                || !(bay.parent.TryGetComp<CompFlickable>()?.SwitchIsOn ?? true)) continue;
            IntVec3 cell = bay.parent.Position;
            if (!cell.InBounds(map) || !cell.Walkable(map) || cell.Fogged(map)
                || (bool)Reserved.Invoke(bay, new object[] { cell, null })) continue;
            if (cell.GetThingList(map).Any(t => t is Pawn
                || (t is Skyfaller && t.def != ThingDefOf.DropPodIncoming))) continue;
            result = cell;
            return true;
        }
        return false;
    }
}

// Persist authorisation on the actual skyfaller, not a transient faction guess at impact.
// This also survives saving between pod creation and contact with the roof.
public sealed class MapComponent_SalvageBayPods : MapComponent
{
    private List<Thing> arrivals = new();
    public MapComponent_SalvageBayPods(Map map) : base(map) { }
    internal void Register(Thing pod) { if (!arrivals.Contains(pod)) arrivals.Add(pod); }
    internal bool Contains(Thing pod) => arrivals.Contains(pod);
    public override void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving) Clean();
        Scribe_Collections.Look(ref arrivals, "salvageBayArrivals", LookMode.Reference);
        // SpawnSetup may not have run yet during PostLoadInit. Do not discard live references.
        if (Scribe.mode == LoadSaveMode.PostLoadInit) { arrivals ??= new(); arrivals.RemoveAll(t => t == null); }
    }
    public override void MapComponentTick() { if (Find.TickManager.TicksGame % 250 == 0) Clean(); }
    private void Clean() => arrivals.RemoveAll(t => t == null || t.Destroyed || !t.Spawned || t.Map != map);
}

[HarmonyPatch(typeof(DropPodUtility), nameof(DropPodUtility.MakeDropPodAt))]
internal static class SalvageBayPodDestination
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    [HarmonyBefore("com.solarweb.Stratum")]
    private static void Prefix(ref IntVec3 c, Map map, ActiveTransporterInfo info, Faction faction,
        out SalvageBayDropPods.Arrival __state)
    {
        __state = SalvageBayDropPods.Current;
        SalvageBayDropPods.Current = null;
        // Only ordinary delivery pods: quest shuttles, crash events and enemy pods retain their paths.
        ThingDef faller = info?.sentTransporterDef?.dropPodFaller ?? faction?.def.dropPodIncoming ?? ThingDefOf.DropPodIncoming;
        if (faller != ThingDefOf.DropPodIncoming || !SalvageBayDropPods.Friendly(info, faction)
            || !SalvageBayDropPods.TryCell(map, out IntVec3 cell)) return;
        c = cell;
        SalvageBayDropPods.Current = new SalvageBayDropPods.Arrival { Map = map, Info = info, Cell = cell };
    }

    [HarmonyFinalizer]
    private static void Finalizer(SalvageBayDropPods.Arrival __state) => SalvageBayDropPods.Current = __state;
}

// Stratum's prefix otherwise relocates the cargo and spawns an empty roof-impact decoy.
// Skip that prefix only for this exact authorised delivery; leave MakeDropPodAt itself intact.
[HarmonyPatch(typeof(SolarWeb.Stratum.Patches.DropPodUtility_Patch), "MakeDropPodAt_Prefix")]
internal static class SalvageBayStratumRouting
{
    private static bool Prefix(Map map, ActiveTransporterInfo info) =>
        SalvageBayDropPods.Current == null || SalvageBayDropPods.Current.Map != map
        || !ReferenceEquals(SalvageBayDropPods.Current.Info, info);
}

[HarmonyPatch(typeof(Skyfaller), nameof(Skyfaller.SpawnSetup))]
internal static class SalvageBayPodRegistration
{
    private static void Postfix(Skyfaller __instance)
    {
        var arrival = SalvageBayDropPods.Current;
        if (arrival == null || __instance.Map != arrival.Map || __instance.Position != arrival.Cell
            || __instance.def != ThingDefOf.DropPodIncoming) return;
        if (__instance.innerContainer.OfType<ActiveTransporter>().Any(t => ReferenceEquals(t.Contents, arrival.Info)))
            arrival.Map.GetComponent<MapComponent_SalvageBayPods>().Register(__instance);
    }
}

[HarmonyPatch(typeof(Skyfaller), "HitRoof")]
internal static class SalvageBayPodRoof
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    [HarmonyBefore("com.solarweb.Stratum")]
    private static bool Prefix(Skyfaller __instance)
    {
        if (__instance.Map?.GetComponent<MapComponent_SalvageBayPods>()?.Contains(__instance) != true) return true;
        // Mark contact complete, then skip both vanilla and Stratum's damaging prefixes.
        // The original Impact/SpawnThings path still owns cargo delivery and world-pawn cleanup.
        __instance.hasHitRoof = true;
        return false;
    }
}
