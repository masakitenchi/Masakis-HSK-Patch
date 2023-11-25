using SK;
using System.Runtime.CompilerServices;
using Verse.Profile;
using System.Xml;

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

    [HarmonyPatch(typeof(ResearchProjectDef),nameof(ResearchProjectDef.ReapplyAllMods))]
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

public static class Log
{
    private static readonly string Prefix = "[Core SK Patch] :: ";
    public static void Message(string message)
    {
        Verse.Log.Message(Prefix + message);
    }

    public static void Warning(string message)
    {
        Verse.Log.Warning(Prefix + message);
    }

    public static void Error(string message)
    {
        Verse.Log.Error(Prefix + message);
    }

    public static void WarningOnce(string message, int seed)
    {
        Verse.Log.WarningOnce(Prefix + message, seed);
    }
}