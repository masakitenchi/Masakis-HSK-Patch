using CombatExtended;

namespace Core_SK_Patch;
#if DEBUG


[HarmonyPatch]
public static class TestPatches
{
    [HarmonyPatch(typeof(ResearchProjectDef),nameof(ResearchProjectDef.ReapplyAllMods))]
    [HarmonyPrefix]
    public static bool Skip(ResearchProjectDef __instance)
    {
        if (__instance.researchMods == null)
        {
            return false ;
        }
        for (int i = 0; i < __instance.researchMods.Count; i++)
        {
            try
            {
                Log.Message($"researchMod: {__instance.researchMods[i].GetType()}");
                __instance.researchMods[i].Apply();
            }
            catch (Exception ex)
            {
                Log.Error(string.Concat("Exception applying research mod for project ", __instance, ": ", ex.ToString()));
            }
        }
        return false;
    }
}
#endif

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

        Log.Message(sb.ToString());
    }
}

[HarmonyPatch]
public static class IncidentWorkerErrorcheck
{
    [HarmonyPatch(typeof(IncidentDef), nameof(IncidentDef.ConfigErrors))]
    [HarmonyPostfix]
    public static IEnumerable<string> Postfix(IEnumerable<string> __result, IncidentDef __instance)
    {
        foreach(var i in __result)
        {
            yield return i;
        }
        if(__instance.workerClass is null)
        {
            yield return $"{__instance.defName} has null workerClass.";
        }
    }
}

//[HarmonyPatch]
public static class CompAffectedByFacilitiesTestPatch
{
    private static readonly Type CF = AccessTools.TypeByName("CF.CompProperties_UnlocksRecipe");
    /*//[HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.Notify_NewLink))]
    public static void Prefix(CompAffectedByFacilities __instance, ref bool __state)
    {
        if (__instance.parent.AllComps.Any(x => x.props.GetType() == CF))
        {
            __state = true;
            Log.Message($"Thing: {__instance.parent}\n LinkedFacilities:\n {string.Join("\n ", __instance.LinkedFacilitiesListForReading.Select(x => x.ToString()))}");
        }
    }


    //[HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.Notify_NewLink))]
    public static void Postfix(CompAffectedByFacilities __instance, ref bool __state)
    {
        if (__state)
        {

            Log.Message($"Thing: {__instance.parent}\n LinkedFacilities:\n {string.Join("\n", __instance.LinkedFacilitiesListForReading.Select(x => x.ToString()))}\n StackTrace:\n " + StackTraceUtility.ExtractStackTrace());
        }
    }
    //[HarmonyPatch(typeof(CompFacility), nameof(CompFacility.LinkToNearbyBuildings))]
    public static void Prefix(CompFacility __instance, ref bool __state)
    {
        Log.Message($"Thing: {__instance.parent}\n LinkedFacilities:\n {string.Join("\n", __instance.LinkedBuildings.Select(x => x.ToString()))}");
    }

    //[HarmonyPatch(typeof(CompFacility), nameof(CompFacility.LinkToNearbyBuildings))]
    public static void Postfix(CompFacility __instance)
    {
        Log.Message($"Thing: {__instance.parent}\n LinkedFacilities:\n {string.Join("\n", __instance.LinkedBuildings.Select(x => x.ToString()))}\n StackTrace:\n " + StackTraceUtility.ExtractStackTrace());
    }*/

    /*[HarmonyPatch(typeof(CompFacility),nameof(CompFacility.PostSpawnSetup))]
    public static bool Prefix(CompFacility __instance, bool respawningAfterLoad)
    {
        if (respawningAfterLoad) return false;
        return true;
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.PostSpawnSetup))]
    public static bool Prefix(CompAffectedByFacilities __instance, bool respawningAfterLoad)
    {
        if(respawningAfterLoad) return false;
        return true;
    }*/
}