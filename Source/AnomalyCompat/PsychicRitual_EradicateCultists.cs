using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Core_SK_Patch;

/// <summary>
/// An invocation-circle ritual with two manually consumed offerings.
/// The game's ritual framework can only make one invoker carry one offering
/// stack, which is not sufficient for the 100 bioferrite requirement.
/// </summary>
public class PsychicRitualDef_EradicateCultists : PsychicRitualDef_InvocationCircle
{
    public IngredientCount additionalOffering;

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        additionalOffering?.ResolveReferences();
    }

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
    {
        IngredientCount primaryOffering = requiredOffering;
        requiredOffering = null;
        try
        {
            List<PsychicRitualToil> toils = base.CreateToils(psychicRitual, graph);
            toils.Add(new PsychicRitualToil_EradicateCultists());
            return toils;
        }
        finally
        {
            requiredOffering = primaryOffering;
        }
    }

    public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
    {
        foreach (string issue in base.BlockingIssues(assignments, map))
        {
            yield return issue;
        }

        if (additionalOffering is null)
        {
            yield break;
        }

        List<Pawn> participants = assignments.AllAssignedPawns.ToList();
        if (!OfferingReachable(map, participants, additionalOffering, out int available))
        {
            yield return "PsychicRitualOfferingsInsufficient".Translate(additionalOffering.Summary, available);
        }
    }

    public override TaggedString TimeAndOfferingLabel()
    {
        if (additionalOffering is null)
        {
            return base.TimeAndOfferingLabel();
        }

        return "CoreSKP_PsychicRitualTimeAndOfferings".Translate(
            DurationLabel(),
            RequiredOffering.Summary,
            additionalOffering.Summary);
    }

    public override TaggedString OutcomeDescription(
        FloatRange qualityRange,
        string qualityNumber,
        PsychicRitualRoleAssignments assignments)
    {
        return outcomeDescription;
    }

    internal bool TryConsumeRequiredOfferings(Map map, out IngredientCount missingOffering)
    {
        IngredientCount[] offerings = { RequiredOffering, additionalOffering };
        foreach (IngredientCount offering in offerings)
        {
            if (offering is not null && AvailableOfferingCount(map, offering) < RequiredOfferingCount(offering))
            {
                missingOffering = offering;
                return false;
            }
        }

        foreach (IngredientCount offering in offerings)
        {
            if (offering is not null)
            {
                ConsumeOffering(map, offering);
            }
        }

        missingOffering = null;
        return true;
    }

    private static int RequiredOfferingCount(IngredientCount offering)
    {
        return Mathf.CeilToInt(offering.GetBaseCount());
    }

    private static int AvailableOfferingCount(Map map, IngredientCount offering)
    {
        return map.listerThings.AllThings
            .Where(thing => !thing.Destroyed && offering.filter.Allows(thing))
            .Sum(thing => thing.stackCount);
    }

    private static void ConsumeOffering(Map map, IngredientCount offering)
    {
        int required = RequiredOfferingCount(offering);
        List<Thing> candidates = map.listerThings.AllThings
            .Where(thing => !thing.Destroyed && offering.filter.Allows(thing))
            .ToList();

        int remaining = required;
        foreach (Thing thing in candidates)
        {
            int amount = Math.Min(remaining, thing.stackCount);
            if (amount == thing.stackCount)
            {
                thing.Destroy();
            }
            else
            {
                thing.SplitOff(amount).Destroy();
            }

            remaining -= amount;
            if (remaining == 0)
            {
                return;
            }
        }
    }
}

public class PsychicRitualRoleDef_MinimumPsychicSensitivity : PsychicRitualRoleDef
{
    public float minimumPsychicSensitivity = 2f;

    private enum FailureReason
    {
        None,
        BelowMinimumPsychicSensitivity
    }

    public override bool PawnCanDo(Context context, Pawn pawn, TargetInfo target, out AnyEnum reason)
    {
        if (!base.PawnCanDo(context, pawn, target, out reason))
        {
            return false;
        }

        if (pawn.GetStatValue(StatDefOf.PsychicSensitivity) < minimumPsychicSensitivity)
        {
            reason = AnyEnum.FromEnum(FailureReason.BelowMinimumPsychicSensitivity);
            return false;
        }

        reason = AnyEnum.None;
        return true;
    }

    public override TaggedString PawnCannotDoReason(AnyEnum reason, Context context, Pawn pawn, TargetInfo target)
    {
        if (reason.As<FailureReason>() == FailureReason.BelowMinimumPsychicSensitivity)
        {
            return "CoreSKP_PsychicRitualInvokerSensitivityTooLow".Translate(
                pawn.Named("PAWN"),
                minimumPsychicSensitivity.ToStringPercent());
        }

        return base.PawnCannotDoReason(reason, context, pawn, target);
    }
}

public class PsychicRitualToil_EradicateCultists : PsychicRitualToil
{
    public PsychicRitualToil_EradicateCultists()
    {
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);

        Map map = psychicRitual.Map;
        PsychicRitualDef_EradicateCultists ritualDef =
            (PsychicRitualDef_EradicateCultists)psychicRitual.def;

        if (!ritualDef.TryConsumeRequiredOfferings(map, out IngredientCount missingOffering))
        {
            psychicRitual.CancelPsychicRitual(
                "CoreSKP_PsychicRitualAdditionalOfferingMissing".Translate(
                    missingOffering.Summary));
            return;
        }

        List<Pawn> cultists = map.lordManager.lords
            .Where(IsActiveCultistRitualLord)
            .SelectMany(lord => lord.ownedPawns)
            .Where(pawn =>
                !pawn.Dead &&
                pawn.Spawned &&
                pawn.Map == map &&
                pawn.Faction?.def == FactionDefOf.HoraxCult)
            .Distinct()
            .ToList();

        foreach (Pawn cultist in cultists)
        {
            RemoveDeathRefusal(cultist);
            cultist.Kill(null, null);
        }

        psychicRitual.ReleaseAllPawnsAndBuildings();

        Find.LetterStack.ReceiveLetter(
            "CoreSKP_EradicateCultistsCompleteLabel".Translate(),
            "CoreSKP_EradicateCultistsCompleteText".Translate(cultists.Count),
            LetterDefOf.PositiveEvent);
    }

    private static bool IsActiveCultistRitualLord(Lord lord)
    {
        if (lord.faction?.def != FactionDefOf.HoraxCult)
        {
            return false;
        }

        if (lord.LordJob is LordJob_PsychicRitual)
        {
            return lord.CurLordToil is LordToil_PsychicRitual;
        }

        if (lord.LordJob is LordJob_HateChant)
        {
            return lord.CurLordToil is LordToil_PsychicRitualParticipantGoto ||
                lord.CurLordToil is LordToil_HateChant;
        }

        return false;
    }

    private static void RemoveDeathRefusal(Pawn pawn)
    {
        if (pawn.health is null)
        {
            return;
        }

        List<Hediff> deathRefusals = pawn.health.hediffSet.hediffs
            .Where(hediff => hediff.def == HediffDefOf.DeathRefusal)
            .ToList();
        foreach (Hediff deathRefusal in deathRefusals)
        {
            pawn.health.RemoveHediff(deathRefusal);
        }
    }
}
