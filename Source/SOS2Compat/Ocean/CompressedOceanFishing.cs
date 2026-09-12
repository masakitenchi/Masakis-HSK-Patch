using Verse.AI;

namespace Core_SK_Patch;

internal static class CompressedOceanFishing
{
    internal const string BuildingDefName = "CoreSK_CompressedOcean";
    internal const string WorkJobName = "CoreSK_FishCompressedOcean";
    internal const string JoyJobName = "CoreSK_RelaxCompressedOcean";

    internal static bool AllowedByIdeology(Pawn pawn) => pawn.Ideo == null
        || new HistoryEvent(HistoryEventDefOf.SlaughteredFish, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo_Job();

    internal static bool CanUse(Pawn pawn, Building_CompressedOcean ocean, bool recreation, bool forced = false)
    {
        if (!ModsConfig.OdysseyActive || ocean == null || ocean.Map != pawn.Map || ocean.Faction != pawn.Faction
            || !ocean.CanFish(recreation) || ocean.IsForbidden(pawn) || ocean.Fogged()
            || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || !AllowedByIdeology(pawn))
            return false;
        IntVec3 stand = ocean.InteractionCell;
        return stand.InBounds(pawn.Map) && stand.Standable(pawn.Map) && !stand.IsForbidden(pawn)
            && !stand.VacuumConcernTo(pawn)
            && pawn.CanReserve(ocean, 1, -1, null, forced)
            && pawn.CanReserveAndReach(stand, PathEndMode.OnCell, Danger.Some, 1, -1, ReservationLayerDefOf.Floor, forced);
    }

    internal static Job MakeJob(Building_CompressedOcean ocean, bool recreation) =>
        JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(recreation ? JoyJobName : WorkJobName), ocean, ocean.InteractionCell);
}

public sealed class WorkGiver_CompressedOcean : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed(CompressedOceanFishing.BuildingDefName));
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        CompressedOceanFishing.CanUse(pawn, t as Building_CompressedOcean, false, forced);
    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        HasJobOnThing(pawn, t, forced) ? CompressedOceanFishing.MakeJob((Building_CompressedOcean)t, false) : null;
}

public sealed class JoyGiver_CompressedOcean : JoyGiver
{
    public override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.needs.joy == null) return null;
        var candidates = new List<Thing>();
        GetSearchSet(pawn, candidates);
        Thing best = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, candidates,
            PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f,
            t => t.IsSociallyProper(pawn) && t.IsPoliticallyProper(pawn)
                && CompressedOceanFishing.CanUse(pawn, t as Building_CompressedOcean, true));
        return best is Building_CompressedOcean ocean ? CompressedOceanFishing.MakeJob(ocean, true) : null;
    }
}

public sealed class JobDriver_CompressedOcean : JobDriver
{
    private int fishingDuration;
    private Building_CompressedOcean Ocean => TargetThingA as Building_CompressedOcean;
    private bool Recreation => job.def.defName == CompressedOceanFishing.JoyJobName;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref fishingDuration, "compressedOceanFishingDuration");
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed)
        && pawn.Reserve(TargetB, job, 1, -1, ReservationLayerDefOf.Floor, errorOnFailed);

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => Ocean == null || !Ocean.CanFish(Recreation)
            || !CompressedOceanFishing.AllowedByIdeology(pawn));
        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
        // Reuse Odyssey's duration/stats/skill/effect, but never invent a fake
        // WaterBody or change the ship floor to water just to satisfy JobDriver_Fish.
        // Keep the original duration across saves; JobDriver serializes the
        // remaining ticks. Rebuilding toils must neither reset progress nor
        // change the progress-bar denominator to a placeholder duration.
        if (fishingDuration <= 0)
            fishingDuration = Mathf.Max(1, Mathf.RoundToInt(Ocean.Properties.fishingTicks
                / Mathf.Max(0.05f, pawn.GetStatValue(StatDefOf.FishingSpeed))));
        Toil fishing = Toils_General.WaitWith(TargetIndex.A, fishingDuration, false, true, false, TargetIndex.A);
        fishing.AddPreTickAction(() =>
        {
            pawn.skills?.Learn(SkillDefOf.Animals, 0.025f);
            pawn.GainComfortFromCellIfPossible(1, chairsOnly: true);
            if (Recreation)
                JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.None, joySource: Ocean);
        });
        fishing.WithEffect(EffecterDefOf.Fishing, TargetIndex.A);
        fishing.WithProgressBarToilDelay(TargetIndex.B);
        yield return fishing;
        Toil complete = ToilMaker.MakeToil("CatchCompressedOceanFish");
        complete.initAction = () => Ocean.TryCatch(pawn, Recreation);
        complete.PlaySoundAtStart(SoundDefOf.Interact_CatchFish);
        yield return complete;
    }
}
