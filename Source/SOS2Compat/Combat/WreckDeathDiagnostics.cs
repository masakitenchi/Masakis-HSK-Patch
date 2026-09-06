using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using SaveOurShip2;
using Verse;

namespace Core_SK_Patch;

internal static class WreckDeathTrace
{
    internal sealed class Generation
    {
        internal string Description;
    }
    internal sealed class Death
    {
        internal Pawn Pawn;
        internal int Hits;
        internal int AbnormalResults;
    }
    [ThreadStatic] internal static Generation Generating;
    [ThreadStatic] internal static Death Dying;

    // Diagnostics must not replace the exception under investigation.
    internal static void Write(Func<string> message)
    {
        try { Log.Message("[SOS2WreckDiag] " + message()); }
        catch (Exception) { /* Never change game behavior because formatting failed. */ }
    }

    internal static string Describe(Pawn p)
    {
        if (p == null) return "pawn=null";
        return $"pawn={p.ThingID} type={p.GetType().FullName} kind={p.kindDef?.defName} race={p.def?.defName}"
            + $" map={p.Map?.uniqueID} pos={p.Position} dead={p.Dead} destroyed={p.Destroyed}"
            + $" healthNull={p.health == null} hediffSetNull={p.health?.hediffSet == null}";
    }

    internal static string Gear(Pawn p)
    {
        string apparel = p?.apparel?.WornApparel == null ? "-" : string.Join(",",
            p.apparel.WornApparel.Select(a => a.def.defName + "[" +
                string.Join("|", a.AllComps.Select(c => c.GetType().FullName)) + "]"));
        string hediffs = p?.health?.hediffSet?.hediffs == null ? "-" : string.Join(",",
            p.health.hediffSet.hediffs.Select(h => h.def.defName + ":" + h.GetType().FullName));
        return $"apparel={apparel} hediffs={hediffs}";
    }

    internal static DamageWorker.DamageResult TakeDamage(Thing thing, DamageInfo info)
    {
        Death trace = Dying;
        bool active = Generating != null && trace != null && ReferenceEquals(thing, trace.Pawn);
        if (active) trace.Hits++;
        DamageWorker.DamageResult result;
        try { result = thing.TakeDamage(info); }
        catch (Exception ex)
        {
            if (active) Write(() => $"TAKE_DAMAGE_THROW hit={trace.Hits} {Describe(trace.Pawn)} exception={ex}");
            throw;
        }
        if (active && (result == null || result.hediffs == null || result.hediffs.Any(h => h == null)))
        {
            trace.AbnormalResults++;
            if (trace.AbnormalResults <= 3)
                Write(() => $"ABNORMAL_RESULT hit={trace.Hits} {Describe(trace.Pawn)} damage={info.Def?.defName}"
                    + $" amount={info.Amount} part={info.HitPart?.def?.defName} resultNull={result == null}"
                    + $" hediffsNull={result?.hediffs == null} hediffCount={result?.hediffs?.Count}"
                    + $" nullEntries={result?.hediffs?.Count(h => h == null)} dealt={result?.totalDamageDealt}"
                    + $" deflected={result?.deflected} {Gear(trace.Pawn)}");
        }
        return result; // Deliberately return null lists unchanged to reproduce the fault.
    }
}

[HarmonyPatch(typeof(ShipInteriorMod2), nameof(ShipInteriorMod2.GenerateShipDef))]
internal static class WreckGenerationDiagnosticScope
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static void Prefix(ShipDef shipDef, Map map, int wreckLevel, out WreckDeathTrace.Generation __state)
    {
        __state = WreckDeathTrace.Generating;
        WreckDeathTrace.Generating = wreckLevel > 0
            ? new WreckDeathTrace.Generation { Description = $"ship={shipDef?.defName} map={map?.uniqueID} wreckLevel={wreckLevel}" }
            : null;
    }

    [HarmonyFinalizer]
    private static void Finalizer(Exception __exception, WreckDeathTrace.Generation __state)
    {
        if (__exception != null && WreckDeathTrace.Generating != null)
            WreckDeathTrace.Write(() => $"GENERATION_THROW {WreckDeathTrace.Generating.Description} exception={__exception}");
        WreckDeathTrace.Generating = __state;
    }
}

[HarmonyPatch(typeof(HealthUtility), nameof(HealthUtility.DamageUntilDead))]
internal static class WreckPawnDeathDiagnostics
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static void Prefix(Pawn p, out WreckDeathTrace.Death __state)
    {
        __state = WreckDeathTrace.Dying;
        WreckDeathTrace.Dying = WreckDeathTrace.Generating == null ? null : new WreckDeathTrace.Death { Pawn = p };
        if (WreckDeathTrace.Dying != null)
            WreckDeathTrace.Write(() => $"DEATH_BEGIN {WreckDeathTrace.Generating.Description} {WreckDeathTrace.Describe(p)} {WreckDeathTrace.Gear(p)}");
    }

    [HarmonyFinalizer]
    private static void Finalizer(Exception __exception, WreckDeathTrace.Death __state)
    {
        WreckDeathTrace.Death trace = WreckDeathTrace.Dying;
        if (trace != null)
            WreckDeathTrace.Write(() => $"DEATH_END {WreckDeathTrace.Describe(trace.Pawn)} hits={trace.Hits}"
                + $" abnormalResults={trace.AbnormalResults} exception={(__exception == null ? "none" : __exception.ToString())}");
        WreckDeathTrace.Dying = __state;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var target = AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage));
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(target))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(WreckDeathTrace), nameof(WreckDeathTrace.TakeDamage));
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1)
            throw new InvalidOperationException($"SOS2 wreck diagnostics expected one TakeDamage call, found {replaced}.");
    }
}
