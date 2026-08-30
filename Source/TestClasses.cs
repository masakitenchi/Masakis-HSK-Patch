using CombatExtended;

namespace Core_SK_Patch;

/*public static class OutputBuildingTags
{
    [HarmonyPatch(typeof(CombatExtended.Building_TurretGunCE), nameof(CombatExtended.Building_TurretGunCE.GetGizmos))]
    [HarmonyPostfix]
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CombatExtended.Building_TurretGunCE __instance)
    {
        foreach (var result in __result)
            yield return result;
        if (Prefs.DevMode)
            yield return new Command_Action()
            {
                action = () =>
                {
                    Log.Message(string.Join("\n", __instance.def.building.buildingTags.Select(x => x)));
                },
                defaultLabel = "DEV: output tags",
            };
    }
}*/

//[HarmonyPatch]
[StaticConstructorOnStartup]
public class ErrorChecker
{
    static ErrorChecker()
    {
        StringBuilder sb = new StringBuilder();
        #region Viles_Fix
        sb.AppendLine("Checking plants that's harvestable but don't have harvestedThingDef. Setting these plants' harvestYield to 0.");
        foreach (var plant in DefDatabase<ThingDef>.AllDefs.Where(x => x.plant is not null && x.plant.Harvestable && x.plant.harvestedThingDef == null))
        {
            sb.AppendLine(" - " + plant.defName);
            plant.plant.harvestYield = 0;
        }
        sb.AppendLine();
        #endregion

        /*#region StuffTest
        sb.AppendLine("Checking if null stuffAdjective really can make things LabelAsStuff to be its label:");
        foreach(var thing in DefDatabase<ThingDef>.AllDefs.Where(x => x.stuffProps is not null))
        {
            if (thing.stuffProps.stuffAdjective.NullOrEmpty() && thing.LabelAsStuff != thing.label)
                sb.AppendLine(" - " + thing.defName);
        }
        #endregion*/

        /*sb.AppendLine("Checking DamageDefs that's probably missing DamageDefExtensionCE.");
        foreach(var dmg in DefDatabase<DamageDef>.AllDefs.Where(x => x.armorCategory is not null && (x.armorCategory.defName == "Heat" || x.armorCategory.defName == "Electric")))
        {
            if (dmg.GetModExtension<DamageDefExtensionCE>() is null || !dmg.GetModExtension<DamageDefExtensionCE>().isAmbientDamage)
                sb.AppendLine(" - " + dmg.defName + $" ({dmg.modContentPack.Name})");
        }*/

        Logger.Message(sb.ToString());
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
