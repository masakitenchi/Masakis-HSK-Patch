using SK;
using System.Runtime.CompilerServices;
using Verse.Profile;

namespace Core_SK_Patch;

public enum TargetMode
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Set
};

public static class Utilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Invert_Bool(ref this bool a)
    {
        a = !a;
    }
}

[HarmonyPatch]
public static class ResearchScriber
{
    //Every ResearchProjectDef has a HashSet of ResearchMods to apply
    public static Dictionary<ResearchProjectDef, HashSet<ResearchMod>> modLister = new();

    [HarmonyPatch(typeof(ResearchProjectDef),nameof(ResearchProjectDef.ReapplyAllMods))]
    [HarmonyPrefix]
    public static bool DoNotApplyModMoreThanOnce(ResearchProjectDef __instance)
    {
        return modLister.TryAdd(__instance, __instance.researchMods?.ToHashSet());
    }

    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    [HarmonyPrefix]
    public static void ResetAllFieldsAndCleanCache()
    {
        modLister.Values.Do(x =>
            {
                foreach (var mod in x)
                {
                    switch(mod)
                    {
                        case ResearchMod_ManipulateField a:
                            a.ResetField();
                            break;
                    }
                }
            });
        modLister.Clear();
    }
}