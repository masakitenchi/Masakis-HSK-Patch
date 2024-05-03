using Verse.AI;
namespace Core_SK_Patch;

[HarmonyPatch]
public static class Patch_ManhunterHostileToEveryHumanBeing
{
    [HarmonyPatch(typeof(MentalState_Manhunter), nameof(MentalState_Manhunter.ForceHostileTo), new Type[] { typeof(Thing) })]
    [HarmonyPrefix]
    public static bool HostileToAnyHumanBeing(ref bool __result, Thing t)
    {
        __result = t is Pawn pawn && pawn.RaceProps.Humanlike;
        return false;
    }
}
