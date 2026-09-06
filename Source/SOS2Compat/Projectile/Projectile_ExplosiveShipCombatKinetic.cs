using System;
using System.Linq;
using HarmonyLib;
using SaveOurShip2;
using UnityEngine;
using Verse;

namespace Core_SK_Patch;

internal static class KineticExplosionGeometry
{
    internal const float ConeDegrees = 30f;
    internal static float Length(float originalRadius) => originalRadius * (float)Math.Sqrt(360f / ConeDegrees);

    internal static bool Contains(float x, float z, float forwardX, float forwardZ, float radius)
    {
        double distanceSquared = (double)x * x + (double)z * z;
        if (distanceSquared > (double)radius * radius + 0.00001)
            return false;
        if (distanceSquared < 0.00001)
            return true; // Include the impact cell.
        double forwardSquared = (double)forwardX * forwardX + (double)forwardZ * forwardZ;
        double dot = (double)x * forwardX + (double)z * forwardZ;
        double cos = Math.Cos(ConeDegrees * Math.PI / 360.0);
        return forwardSquared > 0.00001 && dot > 0
            && dot * dot + 0.00001 >= distanceSquared * forwardSquared * cos * cos;
    }

    internal static bool Applies(string defName) => defName == "Proj_ShipTurretKinetic"
        || defName == "Proj_ShipTurretKinetic_Large";
}

// Keep native projectile classes, Impact/Destroy hooks, damage and serialization.
// Carry direction across the synchronous Explode -> StartExplosion call only.
[HarmonyPatch(typeof(Projectile_Explosive), "Explode")]
internal static class KineticProjectileExplosionScope
{
    internal sealed class Context
    {
        internal ThingDef Def;
        internal Map Map;
        internal IntVec3 Center;
        internal Vector3 Forward;
    }

    [ThreadStatic] internal static Context Current;

    [HarmonyPrefix]
    private static void Prefix(Projectile_Explosive __instance, out Context __state)
    {
        __state = Current;
        Current = null; // Nested unrelated projectiles cannot inherit this scope.
        if (__instance is not Projectile_ExplosiveShip || !KineticExplosionGeometry.Applies(__instance.def.defName))
            return;
        Vector3 forward = __instance.destination - __instance.origin;
        if (forward.x * forward.x + forward.z * forward.z < 0.00001f)
            return; // No usable trajectory: preserve upstream behavior.
        Current = new Context { Def = __instance.def, Map = __instance.Map, Center = __instance.Position, Forward = forward };
    }

    [HarmonyFinalizer]
    private static void Finalizer(Context __state)
    {
        Current = __state; // Restore on exceptions too; never suppress them.
    }
}

[HarmonyPatch(typeof(Explosion), nameof(Explosion.StartExplosion))]
internal static class KineticExplosionCellsPatch
{
    [HarmonyPrefix]
    private static void Prefix(Explosion __instance)
    {
        KineticProjectileExplosionScope.Context context = KineticProjectileExplosionScope.Current;
        if (context == null || __instance.projectile != context.Def || __instance.Map != context.Map
            || __instance.Position != context.Center)
            return;
        KineticProjectileExplosionScope.Current = null; // Do not transform secondary explosions.
        if (__instance.radius <= 0 || (__instance.overrideCells != null && __instance.overrideCells.Count > 0)
            || __instance.affectedAngle.HasValue)
            return; // Respect explicitly supplied shapes from other mods.

        float length = KineticExplosionGeometry.Length(__instance.radius);
        // Native worker supplies LOS, directional LOS and adjacent wall faces.
        // Filter its result rather than all cells in the radius: hulls still block.
        var cells = __instance.damType.Worker.ExplosionCellsToHit(__instance.Position, __instance.Map, length,
                __instance.needLOSToCell1, __instance.needLOSToCell2, null)
            .Where(cell => KineticExplosionGeometry.Contains(cell.x - context.Center.x, cell.z - context.Center.z,
                context.Forward.x, context.Forward.z, length)).Distinct().ToList();
        // Empty overrides fall back to a circle; always include the impact cell.
        if (cells.Count == 0 && context.Center.InBounds(__instance.Map))
            cells.Add(context.Center);
        __instance.radius = length;
        __instance.overrideCells = cells;
        __instance.applyDamageToExplosionCellsNeighbors = false;
    }
}
