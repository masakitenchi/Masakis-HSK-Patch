namespace Core_SK_Patch;

public class PawnGroupKindWorker_Coroutine : PawnGroupKindWorker
{

    public override float MinPointsToGenerateAnything(PawnGroupMaker groupMaker, FactionDef faction, PawnGroupMakerParms parms = null)
    {
        float num = parms == null || (double)parms.points <= 0.0 ? 100000f : parms.points;
        float generateAnything = float.MaxValue;
        List<PawnGenOptionWithXenotype> options = PawnGroupMakerUtility.GetOptions(parms, faction, groupMaker.options, num, num, new float?(float.MaxValue));
        foreach (PawnGenOptionWithXenotype optionWithXenotype in options)
        {
            if (optionWithXenotype.Option.kind.isFighter && (double)optionWithXenotype.Cost < (double)generateAnything && PawnGroupMakerUtility.PawnGenOptionValid(optionWithXenotype.Option, parms))
                generateAnything = optionWithXenotype.Cost;
        }
        if ((double)generateAnything == 3.4028234663852886E+38)
        {
            foreach (PawnGenOptionWithXenotype optionWithXenotype in options)
            {
                if ((double)optionWithXenotype.Cost < (double)generateAnything && PawnGroupMakerUtility.PawnGenOptionValid(optionWithXenotype.Option, parms))
                    generateAnything = optionWithXenotype.Cost;
            }
        }
        return generateAnything;
    }


    new public List<Pawn> GeneratePawns(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, bool errorOnZeroResults = true)
    {
        List<Pawn> outPawns = new List<Pawn>();
        PawnGroupKindWorker.pawnsBeingGeneratedNow.Add(outPawns);
        try
        {
            this.GeneratePawns(parms, groupMaker, outPawns, errorOnZeroResults);
        }
        catch (Exception ex)
        {
            Log.Error("Exception while generating pawn group: " + (object)ex);
            for (int index = 0; index < outPawns.Count; ++index)
                outPawns[index].Destroy(DestroyMode.Vanish);
            outPawns.Clear();
        }
        finally
        {
            PawnGroupKindWorker.pawnsBeingGeneratedNow.Remove(outPawns);
        }
        return outPawns;
    }

    protected override void GeneratePawns(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults)
    {
        if (!this.CanGenerateFrom(parms, groupMaker))
        {
            if (!errorOnZeroResults)
                return;
            Log.Error("Cannot generate pawns for " + (object)parms.faction + " with " + (object)parms.points + ". Defaulting to a single random cheap group.");
        }
        else
        {
            bool flag1 = parms.raidStrategy == null || parms.raidStrategy.pawnsCanBringFood || parms.faction != null && !parms.faction.HostileTo(Faction.OfPlayer);
            Predicate<Pawn> predicate = parms.raidStrategy != null ? (Predicate<Pawn>)(p => parms.raidStrategy.Worker.CanUsePawn(parms.points, p, outPawns)) : (Predicate<Pawn>)null;
            bool flag2 = false;
            foreach (PawnGenOptionWithXenotype genOptionsByPoint in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(parms.points, groupMaker.options, parms))
            {
                PawnKindDef kind = genOptionsByPoint.Option.kind;
                Faction faction = parms.faction;
                Ideo ideo = parms.ideo;
                XenotypeDef xenotype = genOptionsByPoint.Xenotype;
                int tile = parms.tile;
                int num1 = flag1 ? 1 : 0;
                int num2 = parms.inhabitants ? 1 : 0;
                Predicate<Pawn> validatorPostGear = predicate;
                float? minChanceToRedressWorldPawn = new float?();
                float? fixedBiologicalAge = new float?();
                float? fixedChronologicalAge = new float?();
                Gender? fixedGender = new Gender?();
                Ideo fixedIdeo = ideo;
                XenotypeDef forcedXenotype = xenotype;
                FloatRange? excludeBiologicalAgeRange = new FloatRange?();
                FloatRange? biologicalAgeRange = new FloatRange?();
                PawnGenerationRequest request = new PawnGenerationRequest(kind, faction, tile: tile, mustBeCapableOfViolence: true, allowPregnant: true, allowFood: (num1 != 0), inhabitant: (num2 != 0), validatorPostGear: validatorPostGear, minChanceToRedressWorldPawn: minChanceToRedressWorldPawn, fixedBiologicalAge: fixedBiologicalAge, fixedChronologicalAge: fixedChronologicalAge, fixedGender: fixedGender, fixedIdeo: fixedIdeo, forcedXenotype: forcedXenotype, excludeBiologicalAgeRange: excludeBiologicalAgeRange, biologicalAgeRange: biologicalAgeRange);
                if (parms.raidAgeRestriction != null && parms.raidAgeRestriction.Worker.ShouldApplyToKind(genOptionsByPoint.Option.kind))
                {
                    request.BiologicalAgeRange = parms.raidAgeRestriction.ageRange;
                    request.AllowedDevelopmentalStages = parms.raidAgeRestriction.developmentStage;
                }
                if (genOptionsByPoint.Option.kind.pawnGroupDevelopmentStage.HasValue)
                    request.AllowedDevelopmentalStages = genOptionsByPoint.Option.kind.pawnGroupDevelopmentStage.Value;
                if (!Find.Storyteller.difficulty.ChildRaidersAllowed && parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer))
                    request.AllowedDevelopmentalStages = DevelopmentalStage.Adult;
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                if (parms.forceOneDowned && !flag2)
                {
                    pawn.health.forceDowned = true;
                    if (pawn.guest != null)
                        pawn.guest.Recruitable = true;
                    pawn.mindState.canFleeIndividual = false;
                    flag2 = true;
                }
                outPawns.Add(pawn);
            }
        }
    }
    public override IEnumerable<PawnKindDef> GeneratePawnKindsExample(
      PawnGroupMakerParms parms,
      PawnGroupMaker groupMaker)
    {
        foreach (PawnGenOptionWithXenotype genOptionsByPoint in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(parms.points, groupMaker.options, parms))
            yield return genOptionsByPoint.Option.kind;
    }
}

