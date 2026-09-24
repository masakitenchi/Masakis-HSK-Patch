namespace Core_SK_Patch;

[StaticConstructorOnStartup]
public class ErrorChecker
{
    static ErrorChecker()
    {
        // Some patched plants remain harvestable without a product.
        foreach (var plant in DefDatabase<ThingDef>.AllDefs.Where(x => x.plant is not null && x.plant.Harvestable && x.plant.harvestedThingDef == null))
            plant.plant.harvestYield = 0;
    }
}

[HarmonyPatch]
public static class IncidentWorkerErrorcheck
{
    [HarmonyPatch(typeof(IncidentDef), nameof(IncidentDef.ConfigErrors))]
    [HarmonyPostfix]
    public static IEnumerable<string> Postfix(IEnumerable<string> __result, IncidentDef __instance)
    {
        foreach (var i in __result)
        {
            yield return i;
        }
        if (__instance.workerClass is null)
        {
            yield return $"{__instance.defName} has null workerClass.";
        }
    }
}
