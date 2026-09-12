using Verse.AI;

namespace Core_SK_Patch;

public sealed class CompressedOceanProperties : DefModExtension
{
    public float capacity = 120f;
    public float startingPopulation = 30f;
    public float regenerationPerDay = 24f;
    public float minimumPopulation = 10f;
    public int fishingTicks = 7500;
    public List<ThingDef> fishTypes = new();

    public override IEnumerable<string> ConfigErrors()
    {
        if (capacity <= 0 || startingPopulation < 0 || startingPopulation > capacity
            || regenerationPerDay < 0 || minimumPopulation < 1 || minimumPopulation > capacity || fishingTicks <= 0)
            yield return "Compressed ocean has invalid population or fishing duration settings.";
        if (fishTypes == null || fishTypes.Count == 0 || fishTypes.Any(fish => fish == null || fish.category != ThingCategory.Item || fish.ingestible == null))
            yield return "Compressed ocean requires at least one edible item fish definition.";
    }
}

public sealed class Building_CompressedOcean : Building
{
    private float population = -1f;
    private bool allowWork = true;
    private bool allowRecreation = true;
    private CompPowerTrader power;
    private CompFlickable flick;

    internal CompressedOceanProperties Properties => def.GetModExtension<CompressedOceanProperties>();
    internal float Population => population;
    internal bool Operational => Spawned && ModsConfig.OdysseyActive && power?.PowerOn == true
        && (flick == null || flick.SwitchIsOn) && !this.IsBrokenDown() && !this.IsBurning();
    internal bool CanFish(bool recreation) => Operational && population >= Properties.minimumPopulation
        && (recreation ? allowRecreation : allowWork) && Properties.fishTypes.Count > 0;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        power = GetComp<CompPowerTrader>();
        flick = GetComp<CompFlickable>();
        // Zero is a depleted ocean, not an uninitialized ocean. Preserve it on
        // load, reinstall and SOS2 ship transfers; never seed on every spawn.
        population = population < 0f ? Properties.startingPopulation : OceanStock.Normalize(population, Properties.capacity);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref population, "compressedOceanPopulation", -1f);
        Scribe_Values.Look(ref allowWork, "compressedOceanAllowWork", true);
        Scribe_Values.Look(ref allowRecreation, "compressedOceanAllowRecreation", true);
    }

    public override void TickRare()
    {
        base.TickRare();
        population = OceanStock.Recharge(population, Properties.capacity, Properties.regenerationPerDay,
            GenTicks.TickRareInterval, Operational);
    }

    public override string GetInspectString()
    {
        var lines = new List<string>();
        string original = base.GetInspectString();
        if (!original.NullOrEmpty()) lines.Add(original);
        lines.Add("CoreSK_OceanPopulation".Translate(population.ToString("F1"), Properties.capacity.ToString("F0")));
        lines.Add("CoreSK_OceanRecovery".Translate((Operational ? Properties.regenerationPerDay : 0f).ToString("F0")));
        if (!Operational) lines.Add("CoreSK_OceanOffline".Translate());
        else if (population < Properties.minimumPopulation) lines.Add("CoreSK_OceanRestocking".Translate());
        return string.Join("\n", lines);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
        yield return new Command_Toggle
        {
            defaultLabel = "CoreSK_OceanAllowWork".Translate(),
            defaultDesc = "CoreSK_OceanAllowWorkDesc".Translate(),
            icon = def.uiIcon,
            isActive = () => allowWork,
            toggleAction = () => allowWork = !allowWork
        };
        yield return new Command_Toggle
        {
            defaultLabel = "CoreSK_OceanAllowJoy".Translate(),
            defaultDesc = "CoreSK_OceanAllowJoyDesc".Translate(),
            icon = def.uiIcon,
            isActive = () => allowRecreation,
            toggleAction = () => allowRecreation = !allowRecreation
        };
    }

    internal bool TryCatch(Pawn pawn, bool recreation)
    {
        if (!CanFish(recreation) || pawn?.Map != Map) return false;
        ThingDef fish = Properties.fishTypes.RandomElement();
        float requested = FishingUtility.PopulationToFishYieldCurve.Evaluate(population) * pawn.GetStatValue(StatDefOf.FishingYield);
        int count = OceanStock.CatchCount(population, requested, fish.stackLimit);
        if (count == 0) return false;
        Thing catchThing = ThingMaker.MakeThing(fish);
        catchThing.stackCount = count;
        // Placement can merge part of a stack and still return false. Debit
        // each actual placement in its callback, not the final boolean result.
        int placedCount = 0;
        bool placedAll = GenPlace.TryPlaceThing(catchThing, pawn.Position, Map, ThingPlaceMode.Near,
            placedAction: (_, placed) =>
            {
                placedCount += placed;
                population = OceanStock.Normalize(population - placed, Properties.capacity);
            });
        if (!placedAll && !catchThing.Destroyed && !catchThing.Spawned)
        {
            catchThing.Destroy();
        }
        if (placedCount == 0) return false;
        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.SlaughteredFish, pawn.Named(HistoryEventArgsNames.Doer)));
        return true;
    }
}
