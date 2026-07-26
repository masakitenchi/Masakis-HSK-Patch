using System.Reflection;

namespace Core_SK_Patch;

/// <summary>
/// Rimatomics' threat detector rebuilds every intercepted incident as
/// IncidentDefOf.RaidEnemy while retaining the original faction. Special
/// incidents can use factions without a Combat pawn group maker, so replaying
/// them as a normal raid makes the incident fail after the radar delay.
/// </summary>
[HarmonyPatch]
public static class RimatomicsThreatDetectionCompatibility
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("Rimatomics.RimatomicsResearch:DonkeyRubarb");
    }

    private static bool Prepare()
    {
        return ModsConfig.IsActive("dubwise.rimatomics") && TargetMethod() != null;
    }

    [HarmonyPrefix]
    private static bool AllowOnlyReplayableRaids(IncidentParms parms)
    {
        Faction faction = parms?.faction;
        if (faction?.def?.pawnGroupMakers.NullOrEmpty() != false)
        {
            return false;
        }

        bool hasUsableCombatGroup = faction.def.pawnGroupMakers.Any(
            maker => maker.kindDef == PawnGroupKindDefOf.Combat);

        if (!hasUsableCombatGroup)
        {
            return false;
        }

        return parms.points >= faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat);
    }
}
