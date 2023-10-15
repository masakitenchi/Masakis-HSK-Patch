using CombatExtended;
using Core_SK_Patch;
using SaveOurShip2;
using SK.Enlighten;
using System.Collections;
using System.Security.Cryptography;
using Verse;
using Verse.Sound;

namespace RimWorld;

[HotSwappable]
public class Projectile_ExplosiveShipCombatKinetic : Projectile_ExplosiveShipCombat
{
    //Thinking of a chunk of bullet hit the hull. The energy it carries is unaffected but the explosion it creates should be more "directional".
    private static readonly float _radiusFactor = 5f;
    private int _ExplosionCounter = 5;

    public override void Tick()
    {
        ThingWithCompsReversePatch.Tick(this);
        #region Vanilla Copied
        if (this.landed)
            return;
        Vector3 exactPosition1 = this.ExactPosition;
        --this.ticksToImpact;
        if (!this.ExactPosition.InBounds(this.Map))
        {
            ++this.ticksToImpact;
            this.Position = this.ExactPosition.ToIntVec3();
            this.Destroy(DestroyMode.Vanish);
        }
        else
        {
            Vector3 exactPosition2 = this.ExactPosition;
            if (this.CheckForFreeInterceptBetween(exactPosition1, exactPosition2))
                return;
            this.Position = this.ExactPosition.ToIntVec3();
            if (this.ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && this.def.projectile.soundImpactAnticipate != null)
                this.def.projectile.soundImpactAnticipate.PlayOneShot((SoundInfo)(Thing)this);
            if (this.ticksToImpact <= 0)
            {
                if (this.DestinationCell.InBounds(this.Map))
                    this.Position = this.DestinationCell;
                this.ImpactSomething();
            }
            else
            {
                if (this.ambientSustainer == null)
                    return;
                this.ambientSustainer.Maintain();
            }
        }
        #endregion
    }
    public override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (blockedByShield) this.Destroy();
        /*else if (_ExplosionCounter > 0)
        {
            Log.Message($"Explosion Counter:{_ExplosionCounter}\n destination: {this.destination}\n origin: {this.origin}");
            --this._ExplosionCounter;
            this.Explodeint();
            ++ticksToImpact;
        }*/
        else
            this.Explode();
    }

    public override void Explode() 
    {
        Explodeint();
        Destroy();
        /*Explosion explosion = Utilities.CreateExplosionFrom(this, hitThing.Position, hitThing.Map);
        explosion.intendedTarget = this.intendedTarget.Thing;
        explosion.radius *= _radiusFactor;
        explosion.affectedAngle = range;
        explosion.StartExplosion(this.def.projectile.damageDef.soundExplosion, null);*/
    }

    private void Explodeint()
    {
        //From where the projectile shoots in
        Vector3 origin = this.origin;
        float degrees = Mathf.Atan2(-(this.Position.z - origin.z), (this.Position.x - origin.x)) * Mathf.Rad2Deg;
        //The explosion is restricted into a 30 degrees cone
        FloatRange range = new FloatRange(degrees - 30f, degrees + 30f);
        CoroutineDummy._coroutineDummy.StartCoroutine(CoroutineDummy.ExplosionCoroutine(this.Position,
                                                                                        this.origin,
                                                                                        this.Map,
                                                                                        this.def.projectile.explosionRadius / 3,
                                                                                        def.projectile.damageDef,
                                                                                        this.launcher,
                                                                                        DamageAmount,
                                                                                        ArmorPenetration,
                                                                                        def.projectile.soundExplode,
                                                                                        launcher.def,
                                                                                        def,
                                                                                        intendedTarget.Thing,
                                                                                        affectedAngle: range,
                                                                                        radiusFactor: _radiusFactor,
                                                                                        explosionCount: _ExplosionCounter));
    }
}


[HarmonyPatch]
public static class ThingWithCompsReversePatch
{
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.Tick))]
    [HarmonyReversePatch]
    public static void Tick(ThingWithComps __instance)
    {
        __instance.Tick();
    }
}


internal class CoroutineDummy : MonoBehaviour
{
    internal static CoroutineDummy _coroutineDummy;

    static CoroutineDummy()
    {
        var DummyObject = new GameObject();
        UnityEngine.Object.DontDestroyOnLoad(DummyObject);
        _coroutineDummy = DummyObject.AddComponent<CoroutineDummy>();
    }

    internal static IEnumerator ExplosionCoroutine(IntVec3 center,
                                                   Vector3 origin,
                                                   Map map,
                                                   float radius,
                                                   DamageDef damType,
                                                   Thing instigator,
                                                   int damAmount = -1,
                                                   float armorPenetration = -1,
                                                   SoundDef explosionSound = null,
                                                   ThingDef weapon = null,
                                                   ThingDef projectile = null,
                                                   Thing intendedTarget = null,
                                                   float? direction = null,
                                                   FloatRange? affectedAngle = null,
                                                   float radiusFactor = 1,
                                                   int explosionCount = 1)
    {
        Vector3 directionVec = (center.ToVector3() - origin).normalized;
        directionVec *= radius / 2;
        var ExplosionPos = center;
        var PreviousPos = ExplosionPos;
        for (var i = 0; i < explosionCount; i++)
        {
            ExplosionPos += i * directionVec.ToIntVec3();
            GenExplosion.DoExplosion
                (ExplosionPos,
                 map,
                 radius,
                 damType,
                 instigator,
                 Convert.ToInt32(damAmount * radiusFactor),
                 armorPenetration,
                 explosionSound,
                 weapon,
                 projectile,
                 intendedTarget);
            yield return WaitForFrames(1);
        }
    }

    private static IEnumerator WaitForFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
            yield return null;
    }
}