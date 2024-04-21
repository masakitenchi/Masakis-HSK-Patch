using SK;
using System.Runtime.CompilerServices;
using Verse.Profile;
using System.Xml;
using System.Reflection;
using Steamworks;
using System.IO;

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

    /// <summary>
    /// A simple method for those who only want to tweak a few values. Currently unused due to unsolveable NullReferenceException issue.
    /// </summary>
    /// <param name="proj">The projectile instance to create an explosion from.</param>
    /// <returns>An explosion instance. You need to manually set these parameters:<br/><b>needLOSToCell1<br/>needLOSToCell2<br/>excludeRadius<br/>affectedAngle</b></returns>
    [Obsolete]
    public static Explosion CreateExplosionFrom(Projectile proj, IntVec3 @pos, Map @map)
    {
        ProjectileProperties projectile = proj.def.projectile;
        CompProperties_Explosive comp = proj.def.GetCompProperties<CompProperties_Explosive>();
        Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, pos, map);
        explosion.radius = projectile.explosionRadius;
        explosion.damType = projectile.damageDef;
        explosion.instigator = proj.launcher;
        explosion.damAmount = projectile.damageAmountBase;
        explosion.armorPenetration = projectile.armorPenetrationBase;
        explosion.weapon = proj.equipmentDef;
        explosion.projectile = proj.def;
        //explosion.intendedTarget = proj.intendedTarget.Thing;
        //explosion.preExplosionSpawnThingDef = projectile.preExplosionSpawnThingDef;
        explosion.preExplosionSpawnChance = projectile.preExplosionSpawnChance;
        explosion.preExplosionSpawnThingCount = projectile.preExplosionSpawnThingCount;
        //explosion.postExplosionSpawnThingDef = projectile.postExplosionSpawnThingDef;
        //explosion.postExplosionSpawnThingDefWater = projectile.postExplosionSpawnThingDefWater;
        explosion.postExplosionSpawnChance = projectile.postExplosionSpawnChance;
        explosion.postExplosionSpawnThingCount = projectile.postExplosionSpawnThingCount;
        explosion.postExplosionGasType = projectile.postExplosionGasType;
        explosion.applyDamageToExplosionCellsNeighbors = projectile.applyDamageToExplosionCellsNeighbors;
        explosion.chanceToStartFire = comp.chanceToStartFire;
        explosion.damageFalloff = comp.damageFalloff;
        explosion.doSoundEffects = true;
        explosion.screenShakeFactor = projectile.screenShakeFactor;
        explosion.doVisualEffects = true;
        explosion.propagationSpeed = comp.propagationSpeed;
        return explosion;
    }

    /*[DebugOutput(name = "OutputTerrains")]
    public static void TerrainDefs()
    {
        XmlDocument doc = new XmlDocument();
        doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
        XmlElement Defs = doc.CreateElement("Defs");
        foreach(var def in DefDatabase<TerrainDef>.AllDefs)
        {
            XmlElement defele = doc.CreateElement(def.defName);
            
        }
        doc.AppendChild(Defs);
    }*/
}

[HarmonyPatch]
public static class ResearchScriber
{
    //Every ResearchProjectDef has a HashSet of ResearchMods to apply
    public static Dictionary<ResearchProjectDef, HashSet<ResearchMod>> modLister = new();

    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.ReapplyAllMods))]
    [HarmonyPrefix]
    public static bool DoNotApplyModMoreThanOnce(ResearchProjectDef __instance)
    {
        return modLister.TryAdd(__instance, __instance.researchMods?.ToHashSet() ?? new HashSet<ResearchMod>());
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