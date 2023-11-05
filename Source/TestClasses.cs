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
        foreach(var plant in DefDatabase<ThingDef>.AllDefs.Where(x => x.plant is not null && x.plant.Harvestable && x.plant.harvestedThingDef == null))
        {
            sb.AppendLine(" - " + plant.defName);
            plant.plant.harvestYield = 0;
        }
        sb.AppendLine();
        #endregion
        
        /*sb.AppendLine("Checking DamageDefs that's probably missing DamageDefExtensionCE.");
        foreach(var dmg in DefDatabase<DamageDef>.AllDefs.Where(x => x.armorCategory is not null && (x.armorCategory.defName == "Heat" || x.armorCategory.defName == "Electric")))
        {
            if (dmg.GetModExtension<DamageDefExtensionCE>() is null || !dmg.GetModExtension<DamageDefExtensionCE>().isAmbientDamage)
                sb.AppendLine(" - " + dmg.defName + $" ({dmg.modContentPack.Name})");
        }*/

        Log.Message(sb.ToString());
    }
}

/*[HarmonyPatch]
public static class FoodUtilityFix
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.HarvestableNow), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Postfix(Plant __instance, ref bool __result)
    {
        __result = __instance.def.plant.harvestedThingDef is not null && __result;
    }
}*/