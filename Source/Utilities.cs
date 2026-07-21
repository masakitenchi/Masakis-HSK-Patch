using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using SK;
using Steamworks;
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

[HarmonyPatch]
public static class ResearchScriber
{
    //Every ResearchProjectDef has a HashSet of ResearchMods to apply
    public static Dictionary<string, HashSet<ResearchMod>> modLister = new();

    public static void RegisterAppliedMods(ResearchProjectDef def)
    {
        if (def.researchMods is null || def.researchMods.Count == 0) return;
        modLister[def.defName] = def.researchMods.ToHashSet();
    }

    public static void AllowNextApplication(ResearchProjectDef def)
    {
        modLister.Remove(def.defName);
    }

    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.ReapplyAllMods))]
    [HarmonyPrefix]
    public static bool DoNotApplyModMoreThanOnce(ResearchProjectDef __instance)
    {
        if (__instance.researchMods is null || __instance.researchMods.Count == 0)
        {
            return true;
        }
        if (!modLister.ContainsKey(__instance.defName))
        {
            RegisterAppliedMods(__instance);
#if DEBUG
            Logger.Message($"Applying {__instance.researchMods?.Count} mods for {__instance.defName}");
#endif
            return true;
        }
        return false;
    }

    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    [HarmonyPrefix]
    public static void ResetAllFieldsAndCleanCache()
    {
        modLister.Values.Do(x =>
            {
                foreach (var mod in x)
                {
                    switch (mod)
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

public static class Logger
{
    private static readonly string Prefix = "[Core SK Patch] :: ";
    public static void Message(string message)
    {
        Log.Message(Prefix + message);
    }

    public static void Warning(string message)
    {
        Log.Warning(Prefix + message);
    }

    public static void Error(string message)
    {
        Log.Error(Prefix + message);
    }

    public static void WarningOnce(string message, int seed)
    {
        Log.WarningOnce(Prefix + message, seed);
    }
}


/*[HarmonyPatch]
public static class PatchOperationLogger
{
    private static BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public;
    private static StringBuilder sb = new StringBuilder();
    private static HashSet<Type> allTypes = typeof(PatchOperation).AllSubclassesNonAbstract().Where(x => AccessTools.Field(x, "value") is not null && AccessTools.Field(x, "xpath") is not null).ToHashSet();
    public static XmlWriter writer;

    public static bool Prepare()
    {
        if (!Settings.LogAllPatchOperations) return false;
        if (!allTypes.Any())
        {
            Log.Error("Cannot find any subclasses of PatchOperaion");
            return false;
        }
        Settings.LogAllPatchOperations.Invert_Bool();
        Main.instance.WriteSettings();
        writer = XmlWriter.Create(Path.Combine(GenFilePaths.saveDataPath, "LoggedPatchOperation.xml"));
        Log.Message("Logging all patchoperationpathed. You should be able to find the whole file in config folder");
        return true;
    }

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> PatchOperations()
    {
        return allTypes.Select(x => AccessTools.Method(x, "ApplyWorker"));
    }

    [HarmonyPrefix]
    public static void PreApplyWorker(PatchOperation __instance)
    {
        XmlNode value = __instance.GetInstanceField<XmlContainer>("value").node;
        value.WriteTo(writer);

    }

    [HarmonyPostfix]
    public static void PostApplyWorker(PatchOperation __instance)
    {
        XmlNode value = __instance.GetInstanceField<XmlContainer>("value").node;
        value.WriteTo(writer);
    }
}*/
