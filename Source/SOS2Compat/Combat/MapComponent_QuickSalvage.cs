using SaveOurShip2;

namespace Core_SK_Patch;

// Unlike the old static Salvage list, this owner survives closing the dialog and saving.
public sealed class MapComponent_QuickSalvage : MapComponent, IThingHolder
{
    internal ThingOwner<Thing> Cargo;
    internal bool Prepared, Resolved;

    public MapComponent_QuickSalvage(Map map) : base(map) => Cargo = new ThingOwner<Thing>(this);
    public ThingOwner GetDirectlyHeldThings() => Cargo;
    public IThingHolder ParentHolder => map;
    public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children, Cargo);
    public override void ExposeData()
    {
        Scribe_Values.Look(ref Prepared, "quickSalvagePrepared");
        Scribe_Values.Look(ref Resolved, "quickSalvageResolved");
        Scribe_Deep.Look(ref Cargo, "quickSalvageCargo", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && Cargo == null) Cargo = new ThingOwner<Thing>(this);
    }

    internal void Prepare()
    {
        if (Prepared || Resolved) return;
        ThingDef salvageChunk = DefDatabase<ThingDef>.GetNamed("ShipChunkSalvage");
        var comp = map.GetComponent<ShipMapComp>();
        bool oldCacheOff = comp.CacheOff;
        var refunds = new Dictionary<ThingDef, int>();
        int steel = 0, spacer = 0;
        var snapshot = map.listerThings.AllThings.Where(t => t.Spawned).ToList();
        Prepared = true;
        try
        {
            ProcessPawns();
            foreach (Thing thing in snapshot.Where(t => t is not Building && t is not Pawn))
            {
                if (!thing.Spawned || thing.Destroyed || thing.def.category != ThingCategory.Item
                    || thing.TryGetComp<CompExplosive>() != null) continue;
                thing.TakeDamage(new DamageInfo(DamageDefOf.Crush, Rand.Range(1, 100)));
                if (!thing.Destroyed && thing.Spawned) Store(thing);
            }
            comp.CacheOff = true;
            foreach (Building building in snapshot.OfType<Building>()
                .OrderBy(b => b.def.building.shipPart ? 1 : 0).ThenBy(b => b.thingIDNumber))
            {
                if (!building.Spawned || building.Destroyed) continue;
                if (building.def.Minifiable) Store(building.MakeMinified());
                else
                {
                    var costs = building.CostListAdjusted().ToList();
                    // Refund explicitly: native Deconstruct would apply different rates twice.
                    building.Destroy(DestroyMode.Vanish);
                    foreach (ThingDefCountClass cost in costs)
                    {
                        if (cost.thingDef == ThingDefOf.ComponentSpacer) spacer += cost.count;
                        else if (cost.thingDef == ThingDefOf.Steel) steel += LegacySalvageRules.Refund(cost.count, false);
                        else
                        {
                            refunds.TryGetValue(cost.thingDef, out int count);
                            refunds[cost.thingDef] = count + LegacySalvageRules.Refund(cost.count, cost.thingDef == ThingDefOf.Plasteel);
                        }
                    }
                }
                // Destruction can release casket occupants or vehicle passengers.
                ProcessPawns();
            }
        }
        catch (Exception ex)
        {
            Log.Error("[CoreSK QuickSalvage] Preparation stopped. Remaining structures stay on the source map. " + ex);
        }
        finally
        {
            try
            {
                foreach (var refund in refunds) AddStacks(refund.Key, refund.Value);
                AddStacks(ThingDefOf.ChunkSlagSteel, LegacySalvageRules.SteelSlag(steel));
                for (int i = 0; i < LegacySalvageRules.SalvageChunks(spacer); i++)
                    Store(ThingMaker.MakeThing(salvageChunk).MakeMinified());
            }
            finally
            {
                try { if (comp.CacheOff != oldCacheOff) QuickSalvage.RebuildCache(comp, map); }
                finally { comp.CacheOff = oldCacheOff; }
            }
        }
    }

    private void ProcessPawns()
    {
        // Repeat for passengers released while damaging another pawn; each object handled once.
        var handled = new HashSet<Pawn>();
        while (true)
        {
            var pawns = map.mapPawns.AllPawnsSpawned.Where(p => !handled.Contains(p)).ToList();
            if (pawns.Count == 0) break;
            foreach (Pawn pawn in pawns)
            {
                handled.Add(pawn);
                if (!pawn.Spawned || pawn.Destroyed) continue;
                if (pawn.CanSurviveVacuum()) HealthUtility.DamageUntilDowned(pawn);
                else WreckDeathTrace.DamageUntilDeadForSalvage(pawn);
                Thing recovered = pawn.Dead ? pawn.Corpse : pawn;
                if (recovered != null && !recovered.Destroyed) Store(recovered);
            }
        }
    }

    private void AddStacks(ThingDef def, int count)
    {
        while (count > 0)
        {
            int amount = Math.Min(count, def.stackLimit);
            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = amount;
            Store(thing);
            count -= amount;
        }
    }

    private void Store(Thing thing)
    {
        IntVec3 origin = thing.Spawned ? thing.Position : map.Center;
        if (thing.Spawned) thing.DeSpawn();
        if (Cargo.TryAddOrTransfer(thing)) return;
        if (!thing.Spawned && !thing.Destroyed) GenPlace.TryPlaceThing(thing, origin, map, ThingPlaceMode.Near);
        throw new InvalidOperationException("Could not store salvage: " + thing);
    }

    internal void Finish()
    {
        Resolved = true;
        foreach (Thing thing in Cargo.ToList())
        {
            Cargo.Remove(thing);
            try { GenPlace.TryPlaceThing(thing, map.Center, map, ThingPlaceMode.Near); }
            catch (Exception ex) { Log.Error("[CoreSK QuickSalvage] Could not release abandoned cargo: " + ex); }
            finally { if (!thing.Destroyed && !thing.Spawned) Cargo.TryAddOrTransfer(thing); }
        }
    }
}
