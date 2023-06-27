
#if DEBUG


namespace Core_SK_Patch;
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