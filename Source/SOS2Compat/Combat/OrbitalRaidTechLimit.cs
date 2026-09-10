using SaveOurShip2;

namespace Core_SK_Patch;

internal static class OrbitalRaidTechLimit
{
    internal static bool Blocks(Faction faction, IncidentParms parms) =>
        faction?.def != null && faction.def.techLevel < TechLevel.Spacer &&
        parms?.target is Map map && map.IsSpace();
}

[HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
internal static class OrbitalRaidCanFirePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
    {
        if (__instance is IncidentWorker_RaidEnemy && OrbitalRaidTechLimit.Blocks(parms.faction, parms))
            __result = false;
    }
}

[HarmonyPatch(typeof(IncidentWorker_RaidEnemy), nameof(IncidentWorker_RaidEnemy.FactionCanBeGroupSource))]
internal static class OrbitalRaidFactionCandidatePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Faction f, IncidentParms parms, ref bool __result)
    {
        if (OrbitalRaidTechLimit.Blocks(f, parms))
            __result = false;
    }
}

// Direct TryExecute calls (including RimPacts) need not call CanFireNow.
// Reject an explicit or newly selected faction before pawn generation starts.
[HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryResolveRaidFaction")]
internal static class OrbitalRaidResolvedFactionPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(IncidentParms parms, ref bool __result)
    {
        if (OrbitalRaidTechLimit.Blocks(parms.faction, parms))
            __result = false;
    }
}
