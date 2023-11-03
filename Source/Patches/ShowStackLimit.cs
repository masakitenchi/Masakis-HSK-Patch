namespace Core_SK_Patch;

[HarmonyPatch]
public static class ShowStackLimit
{
    //private static readonly StatDef stackLimit = DefDatabase<StatDef>.GetNamed("StackLimit");
    /*public static bool Prepare()
    {
        return true;
    }*/

    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
    [HarmonyPostfix]
    public static IEnumerable<StatDrawEntry> ShowStackLimit_PassThroughPostfix(IEnumerable<StatDrawEntry> __result, StatRequest req)
    {
        //Log.Message($"StatRequest for {req.defInt.defName}");
        foreach (var entry in __result)
            yield return entry;
        if (req.Pawn is null && req.HasThing && req.thingInt.def.stackLimit != 1)
           yield return new StatDrawEntry(StatCategoryDefOf.BasicsNonPawnImportant, (string)"CSP_StackLimit".Translate(), Convert.ToString(req.thingInt.def.stackLimit), (string)"CSP_StackLimitDesc".Translate(), 500);
    }
}
