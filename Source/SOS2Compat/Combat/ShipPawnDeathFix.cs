using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SaveOurShip2;
using Verse;

namespace Core_SK_Patch;

internal static class WreckDeathTrace
{
    // Preserve nested damage scopes for wreck generation and quick salvage.
    internal sealed class Generation { }
    internal sealed class Death { }
    [ThreadStatic] internal static Generation Generating;
    [ThreadStatic] internal static Death Dying;
    [ThreadStatic] private static bool salvaging;
    internal static bool HandlingShipPawns => Generating != null || salvaging;
    private static readonly List<Hediff> EmptyInjuries = new();

    internal static void DamageUntilDeadForSalvage(Pawn pawn)
    {
        bool previous = salvaging;
        salvaging = true;
        try { HealthUtility.DamageUntilDead(pawn); }
        finally { salvaging = previous; }
    }

    // Replace only the list read for this enumeration. Do not mutate DamageResult.
    internal static List<Hediff> InjuriesForEnumeration(DamageWorker.DamageResult result)
    {
        List<Hediff> injuries = result.hediffs; // A null result remains an error.
        if (injuries == null && HandlingShipPawns && Dying != null)
        {
            return EmptyInjuries;
        }
        return injuries;
    }
}

[HarmonyPatch(typeof(ShipInteriorMod2), nameof(ShipInteriorMod2.GenerateShipDef))]
internal static class WreckGenerationScope
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static void Prefix(int wreckLevel, out WreckDeathTrace.Generation __state)
    {
        __state = WreckDeathTrace.Generating;
        WreckDeathTrace.Generating = wreckLevel > 0
            ? new WreckDeathTrace.Generation()
            : null;
    }

    [HarmonyFinalizer]
    private static void Finalizer(WreckDeathTrace.Generation __state)
    {
        WreckDeathTrace.Generating = __state;
    }
}

[HarmonyPatch(typeof(HealthUtility), nameof(HealthUtility.DamageUntilDead))]
internal static class WreckPawnDeathPatch
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static void Prefix(out WreckDeathTrace.Death __state)
    {
        __state = WreckDeathTrace.Dying;
        WreckDeathTrace.Dying = WreckDeathTrace.HandlingShipPawns ? new WreckDeathTrace.Death() : null;
    }

    [HarmonyFinalizer]
    private static void Finalizer(WreckDeathTrace.Death __state)
    {
        WreckDeathTrace.Dying = __state;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var injuriesField = AccessTools.Field(typeof(DamageWorker.DamageResult), nameof(DamageWorker.DamageResult.hediffs));
        int listsReplaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, injuriesField))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(WreckDeathTrace), nameof(WreckDeathTrace.InjuriesForEnumeration));
                listsReplaced++;
            }
            yield return instruction;
        }
        if (listsReplaced != 1)
            throw new InvalidOperationException($"SOS2 wreck patch expected one hediffs read, found {listsReplaced}.");
    }
}
