using CombatExtended;
using Core_SK_Patch;
using SaveOurShip2;
using Verse;

namespace RimWorld;

public class Projectile_ExplosiveShipCombatKinetic : Projectile_ExplosiveShipCombat
{
    //Thinking of a chunk of bullet hit the hull. The energy it carries is unaffected but the explosion it creates should be more "directional".
    private static readonly float _radiusFactor = 1.5f;
    private int _ExplosionCounter = 5;

    public override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (blockedByShield) this.Destroy();
        else if (_ExplosionCounter > 0)
        {
            --this._ExplosionCounter;
            this.Explodeint();
        }
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
        //From where the projectile flew in
        Vector3 origin = this.origin;
        float degrees = Mathf.Atan2(-(this.Position.z - origin.z), (this.Position.x - origin.x)) * Mathf.Rad2Deg;
        //The explosion is restricted into a 30 degrees cone
        FloatRange range = new FloatRange(degrees - 15f, degrees + 15f);
        GenExplosion.DoExplosion(this.Position, this.Map, this.def.projectile.explosionRadius, def.projectile.damageDef, this.launcher, (int)(DamageAmount * _radiusFactor), ArmorPenetration, def.projectile.soundExplode, launcher.def, def, intendedTarget.Thing);
    }
}
