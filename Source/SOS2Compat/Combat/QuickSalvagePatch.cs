using SaveOurShip2;

namespace Core_SK_Patch;

[HarmonyPatch(typeof(CompShipBaySalvage), nameof(CompShipBaySalvage.CompGetGizmosExtra))]
internal static class QuickSalvagePatch
{
    private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompShipBaySalvage __instance)
    {
        foreach (Gizmo gizmo in __result) yield return gizmo;
        if (__instance.parent.Faction != Faction.OfPlayer || !__instance.parent.Spawned
            || !__instance.parent.Map.IsSpace()) yield break;
        var command = new Command_Action
        {
            defaultLabel = "CoreSK_QuickSalvage".Translate(),
            defaultDesc = "CoreSK_QuickSalvageDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/SalvageShip"),
            action = () => QuickSalvage.ChooseMap(__instance)
        };
        if (!QuickSalvage.Ready(__instance)) command.Disable("CoreSK_QuickSalvageUnavailable".Translate());
        yield return command;
    }
}

internal static class QuickSalvage
{
    // Avoid a direct Vehicle Framework reference for its VehiclePawn parameter.
    private static readonly System.Reflection.MethodInfo Reserved = AccessTools.Method(typeof(CompShipBay), "IsReservedForOtherShuttle");

    internal static bool Ready(CompShipBaySalvage bay) => bay?.parent != null && bay.parent.Spawned
        && bay.parent.Faction == Faction.OfPlayer && bay.parent.Map.IsSpace()
        && bay.parent.Map.GetComponent<ShipMapComp>()?.ShipMapState == ShipMapState.nominal
        && !ShipInteriorMod2.MoveShipFlag && !bay.parent.IsBurning() && !bay.parent.IsBrokenDown()
        && (bay.parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? true)
        && (bay.parent.TryGetComp<CompFlickable>()?.SwitchIsOn ?? true);

    internal static bool Eligible(CompShipBaySalvage bay, Map source) => Ready(bay)
        && Find.Maps.Contains(source) && source != bay.parent.Map && source.IsSpace()
        && source.GetComponent<ShipMapComp>()?.ShipMapState == ShipMapState.isGraveyard
        && !source.GetComponent<ShipMapComp>().CacheOff
        && !source.GetComponent<MapComponent_QuickSalvage>().Resolved;

    internal static void ChooseMap(CompShipBaySalvage bay)
    {
        if (!Ready(bay)) { Reject(); return; }
        var options = Find.Maps.Where(map => Eligible(bay, map))
            .Select(map => new FloatMenuOption(map.Parent.Label, () => ConfirmMap(bay, map))).ToList();
        if (options.Count == 0) { Reject("CoreSK_QuickSalvageNoWreck"); return; }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static void ConfirmMap(CompShipBaySalvage bay, Map source)
    {
        if (!Eligible(bay, source)) { Reject(); return; }
        var session = source.GetComponent<MapComponent_QuickSalvage>();
        CameraJumper.TryJump(source.Center, source);
        if (session.Prepared) { Find.WindowStack.Add(new Dialog_QuickSalvage(bay, session)); return; }
        if (source.listerThings.AllThings.Any(t => t is Skyfaller)) { Reject(); return; }
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
            "CoreSK_QuickSalvageConfirm".Translate().ToString()
                + "\n\n" + "CoreSK_QuickSalvagePawnWarning".Translate(),
            () => LongEventHandler.QueueLongEvent(() =>
            {
                if (!Eligible(bay, source) || source.listerThings.AllThings.Any(t => t is Skyfaller)) { Reject(); return; }
                session.Prepare();
                Find.WindowStack.Add(new Dialog_QuickSalvage(bay, session));
            }, "CoreSK_QuickSalvageWorking", false, null)));
    }

    private static List<CompShipBaySalvage> Bays(CompShipBaySalvage bay) =>
        bay.parent.Map.GetComponent<ShipMapComp>().Bays.OfType<CompShipBaySalvage>().Where(Ready).ToList();

    internal static float Capacity(CompShipBaySalvage bay) => Ready(bay)
        ? Bays(bay).Count * LegacySalvageRules.MassPerBay : 0f;

    internal static bool Deliver(CompShipBaySalvage bay, MapComponent_QuickSalvage session, List<TransferableOneWay> selected)
    {
        if (!Eligible(bay, session.map) || CollectionsMassCalculator.MassUsageTransferables(selected,
            IgnorePawnsInventoryMode.DontIgnore, includePawnsMass: true) > Capacity(bay)) { Reject(); return false; }
        Map destination = bay.parent.Map;
        var cells = Bays(bay).Select(b => b.bayRect.Where(c => c.InBounds(destination) && c.Walkable(destination)
                && !c.GetThingList(destination).Any(t => t is Pawn || t is Skyfaller)
                && !(bool)(Reserved?.Invoke(b, new object[] { c, null }) ?? true))
            .OrderBy(c => c.DistanceToSquared(b.parent.Position)).DefaultIfEmpty(IntVec3.Invalid).First()).Distinct().ToList();
        if (cells.Count == 0 || cells.Any(c => !c.IsValid)) { Reject("CoreSK_QuickSalvageFull"); return false; }
        var pods = cells.Select(c => new ActiveTransporterInfo { leaveSlag = false }).ToArray();
        int index = 0, moved = 0;
        bool dispatched = false;
        try
        {
            foreach (var row in selected)
            {
                int remaining = row.CountToTransfer;
                foreach (Thing thing in row.things.ToList())
                {
                    if (remaining <= 0) break;
                    int count = Math.Min(remaining, thing.stackCount);
                    if (!session.Cargo.Contains(thing) || session.Cargo.TryTransferToContainer(
                        thing, pods[index++ % pods.Length].innerContainer, count) != count)
                        throw new InvalidOperationException("Salvage cargo changed while packing.");
                    remaining -= count;
                }
                if (remaining != 0) throw new InvalidOperationException("Incomplete salvage selection.");
            }
            for (int i = 0; i < pods.Length; i++)
            {
                if (pods[i].innerContainer.Count == 0) continue;
                // Exact bay cell, no random scatter and no floor-stack capacity test.
                int podCount = pods[i].innerContainer.Sum(t => t.stackCount);
                // This is player-arranged salvage, regardless of the passengers' factions.
                // Both SOS2 and Stratum compatibility must use the sender, not cargo allegiance.
                DropPodUtility.MakeDropPodAt(cells[i], destination, pods[i], Faction.OfPlayer);
                moved += podCount;
                pods[i] = null;
                dispatched = true;
            }
        }
        catch (Exception ex)
        {
            foreach (var pod in pods.Where(p => p != null))
                foreach (Thing thing in pod.innerContainer.ToList())
                    pod.innerContainer.TryTransferToContainer(thing, session.Cargo, thing.stackCount);
            Log.Error("[CoreSK QuickSalvage] Delivery interrupted; undelivered cargo retained. " + ex);
            if (!dispatched) { Reject("CoreSK_QuickSalvageFull"); return false; }
        }
        // Once any cargo leaves, this wreck cannot grant another bay-mass allowance.
        session.Finish();
        CameraJumper.TryJump(bay.parent.Position, destination);
        Messages.Message("CoreSK_QuickSalvageDelivered".Translate(moved), MessageTypeDefOf.NeutralEvent, false);
        return true;
    }

    internal static void RebuildCache(ShipMapComp comp, Map map)
    {
        // Workshop RecacheMap expects unassigned cells and live roots. The local
        // branch wraps this setup in RecacheFromRoots; do it explicitly for both.
        var buildings = map.listerThings.AllThings.OfType<Building>().ToList();
        comp.MapRootListAll.Clear();
        comp.MapRootListAll.AddRange(buildings.OfType<Building_ShipBridge>());
        comp.MapShipCells.Clear();
        foreach (Building building in buildings)
        {
            var part = building.TryGetComp<CompShipCachePart>();
            if (part == null) continue;
            part.cellsUnder = new HashSet<IntVec3>(building.OccupiedRect());
            foreach (IntVec3 cell in part.cellsUnder) comp.MapShipCells[cell] = new Tuple<int, int>(-1, -1);
        }
        comp.RecacheMap();
        comp.heatGridDirty = true;
        comp.breathableZoneDirty = true;
    }

    private static void Reject(string key = "CoreSK_QuickSalvageUnavailable") =>
        Messages.Message(key.Translate(), MessageTypeDefOf.RejectInput, false);
}
