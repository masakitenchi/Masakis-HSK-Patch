using System.Reflection;
using System.Reflection.Emit;
using Verse.AI;

namespace Core_SK_Patch;

/// <summary>
/// Removes the vanilla downed-state guards from taming while leaving other
/// animal interactions and the normal handling-skill check unchanged.
/// </summary>
public static class AllowTamingDownedAnimals
{
    [HarmonyPatch]
    private static class CanInteractPatch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(WorkGiver_InteractAnimal), nameof(WorkGiver_InteractAnimal.CanInteractWithAnimal),
                new[] { typeof(Pawn), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool), typeof(bool), typeof(bool), typeof(bool) });
        }

        public static bool Prefix(
            Pawn pawn,
            Pawn animal,
            ref string jobFailReason,
            bool forced,
            bool ignoreSkillRequirements,
            ref bool __result)
        {
            if (!Settings.AllowTamingDownedAnimals || !animal.Downed || !TameUtility.CanTame(animal))
                return true;

            jobFailReason = null;
            if (!pawn.CanReserve(animal, 1, -1, null, forced))
            {
                __result = false;
                return false;
            }

            int minimumSkill = TrainableUtility.MinimumHandlingSkill(animal);
            if (!ignoreSkillRequirements && minimumSkill > pawn.skills.GetSkill(SkillDefOf.Animals).Level)
            {
                jobFailReason = "AnimalsSkillTooLow".Translate(minimumSkill);
                __result = false;
                return false;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(JobDriver_InteractAnimal), "MakeNewToils", MethodType.Enumerator)]
    private static class InteractAnimalJobPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo replacementDefinition = AccessTools.Method(typeof(AllowTamingDownedAnimals), nameof(FailOnDownedUnlessTamingEnabled));

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.operand is MethodInfo calledMethod
                    && calledMethod.Name == nameof(ToilFailConditions.FailOnDowned)
                    && calledMethod.IsGenericMethod)
                {
                    instruction.operand = replacementDefinition.MakeGenericMethod(calledMethod.GetGenericArguments());
                }

                yield return instruction;
            }
        }
    }

    public static T FailOnDownedUnlessTamingEnabled<T>(T endable, TargetIndex targetIndex) where T : IJobEndable
    {
        return Settings.AllowTamingDownedAnimals && endable is JobDriver_Tame
            ? endable
            : endable.FailOnDowned(targetIndex);
    }

    [HarmonyPatch]
    private static class RecruitToilPatch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Toils_Interpersonal)
                .GetNestedTypes(AccessTools.all)
                .SelectMany(type => type.GetMethods(AccessTools.all))
                .First(method => method.Name == "<TryRecruit>b__0");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo awake = AccessTools.Method(typeof(RestUtility), nameof(RestUtility.Awake));
            MethodInfo replacement = AccessTools.Method(typeof(AllowTamingDownedAnimals), nameof(AwakeOrTameableDowned));

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(awake))
                    instruction.operand = replacement;

                yield return instruction;
            }
        }
    }

    public static bool AwakeOrTameableDowned(Pawn pawn)
    {
        return pawn.Awake()
            || Settings.AllowTamingDownedAnimals && pawn.Downed && TameUtility.CanTame(pawn);
    }

#if RW_1_5
    [HarmonyPatch(typeof(InteractionUtility), nameof(InteractionUtility.CanReceiveInteraction))]
#else
    [HarmonyPatch(typeof(SocialInteractionUtility), nameof(SocialInteractionUtility.CanReceiveInteraction))]
#endif
    private static class ReceiveInteractionPatch
    {
        public static void Postfix(Pawn pawn, InteractionDef interactionDef, ref bool __result)
        {
            if (__result
                || !Settings.AllowTamingDownedAnimals
                || !pawn.Downed
                || interactionDef != InteractionDefOf.TameAttempt
                || !TameUtility.CanTame(pawn))
                return;

            __result = !pawn.IsBurning()
#if !RW_1_5
                && !(pawn.IsMutant && pawn.mutant.Def.incapableOfSocialInteractions)
#endif
                && !pawn.IsInteractionBlocked(interactionDef, isInitiator: false, isRandom: false);
        }
    }
}
