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

    [HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.SpecialDisplayStats))]
    [HarmonyPostfix]
    public static IEnumerable<StatDrawEntry> ShowNutritionConversionRate(IEnumerable<StatDrawEntry> __result, StatRequest req, RecipeDef __instance)
    {
        foreach (var entry in __result) yield return entry;
        if (__instance.ingredientValueGetterClass == typeof(IngredientValueGetter_Nutrition))
        {
            double needed = __instance.ingredients.Sum(x => x.count);
            double produced = __instance.products[0].thingDef.ingestible.CachedNutrition * __instance.products[0].count;

            yield return new StatDrawEntry(StatCategoryDefOf.BasicsNonPawnImportant, (string)"CSP_ConversionRate".Translate(),
                (produced / needed).ToString("P2"),
                ExplainationPart(needed, produced), 500);
        }

        string ExplainationPart(double needed, double produced)
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("CSP_ConversionRateExplain".Translate());
            sb.Append("CSP_NutritionNeeded".Translate());
            sb.AppendLine(needed.ToString());
            sb.AppendLine("CSP_NutritionProduced".Translate());
            sb.AppendLine(__instance.products[0].Summary + $" = {produced.ToString("F2")}");
            sb.AppendLine("CSP_ConversionRateText".Translate() + (produced / needed).ToString("P2"));
            return sb.ToString();
        }

    }
}
