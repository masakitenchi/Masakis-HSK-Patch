using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SaveOurShip2;
using Verse;

namespace Core_SK_Patch;

// Do not reference CE's SOS2Compat assembly: it shares our assembly name.
internal static class CETurretReferences
{
    private const string WrapperName = "CombatExtended.Compatibility.SOS2Compat.ShipTurretWrapperCE";
    private const string TurretName = "CombatExtended.Compatibility.SOS2Compat.Building_ShipTurretCE";

    internal static Thing RealTurret(Building_ShipTurret turret)
    {
        if (turret?.GetType().FullName == WrapperName)
            return AccessTools.Field(turret.GetType(), "shipTurretCE").GetValue(turret) as Thing;
        return turret;
    }

    internal static Building_ShipTurret Restore(Thing thing)
    {
        Building_ShipTurret turret = thing as Building_ShipTurret;
        if (turret == null && thing?.GetType().FullName == TurretName)
            turret = (Building_ShipTurret)AccessTools.Method(thing.GetType(), "ToBuilding_ShipTurret").Invoke(thing, null);
        RepairHeatComp(turret);
        return turret;
    }

    internal static void RepairHeatComp(Building_ShipTurret turret)
    {
        // CE can create the wrapper during base.SpawnSetup, before assigning its
        // heatComp field. Components already belong to the real building.
        if (turret != null && turret.heatComp == null)
            turret.heatComp = RealTurret(turret)?.TryGetComp<CompShipHeat>();
    }

    internal static void LookTurret(ref Building_ShipTurret turret, string label, bool saveDestroyedThings)
    {
        Thing real = RealTurret(turret);
        // Keep the existing XML label/ID. Cross-reference resolution must request
        // Thing, not Building_ShipTurret, because CE's building is not a subclass.
        Scribe_References.Look(ref real, label, saveDestroyedThings);
        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            turret = Restore(real);
        else if (Scribe.mode == LoadSaveMode.PostLoadInit)
            RepairHeatComp(turret);
    }
}

[HarmonyPatch(typeof(ShipCombatProjectile), nameof(ShipCombatProjectile.ExposeData))]
internal static class ShipProjectileTurretReferencePatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(Scribe_References)
                && method.Name == "Look" && method.IsGenericMethod
                && method.GetGenericArguments()[0] == typeof(Building_ShipTurret)
                && method.GetParameters()[0].ParameterType == typeof(Building_ShipTurret).MakeByRefType())
            {
                instruction.operand = AccessTools.Method(typeof(CETurretReferences), nameof(CETurretReferences.LookTurret));
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1)
            throw new InvalidOperationException($"SOS2 projectile reference patch expected one turret reference call, found {replaced}.");
    }
}

[HarmonyPatch(typeof(SpaceShipCache), nameof(SpaceShipCache.ActualThreatPerSegment))]
internal static class ShipThreatHeatReferencePatch
{
    [HarmonyPrefix]
    private static void Prefix(SpaceShipCache __instance)
    {
        foreach (Building_ShipTurret turret in __instance.Turrets)
            CETurretReferences.RepairHeatComp(turret);
    }
}
