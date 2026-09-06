using System.Reflection;
using System.Runtime.CompilerServices;
using Rimatomics;
using SaveOurShip2;

namespace Core_SK_Patch;

// Temporary observation only. No capacity, temperature, ratio or network writes.
internal static class RimatomicsSOS2BridgeDiagnostics
{
    private sealed class Sample
    {
        internal int Tick = int.MinValue;
        internal int Attempts, Rejected;
        internal double Requested, Added;
    }
    private static readonly ConditionalWeakTable<Map, Sample> Samples = new();
    private static readonly ConditionalWeakTable<ThingWithComps, Sample> Consumers = new();
    internal static string Id(object value) => value == null ? "null" : RuntimeHelpers.GetHashCode(value).ToString("X");

    internal static void Write(Func<string> text)
    {
        try { Log.Message("[SOS2RimatomicsDiag] " + text()); }
        catch (Exception ex) { Log.WarningOnce("[SOS2RimatomicsDiag] Diagnostic formatting failed: " + ex.GetType().Name, 193740021); }
    }

    internal static bool Begin(UniversalPipeMapComp pipeMap, RimatomicsSOS2HeatBridgeDef config)
    {
        Map map = pipeMap?.map;
        if (map == null) return false;
        int tick = Find.TickManager?.TicksGame ?? 0;
        Sample sample = Samples.GetValue(map, _ => new Sample());
        if (sample.Tick != int.MinValue && tick >= sample.Tick && tick - sample.Tick < 300)
            return false;
        Write(() => $"WINDOW tick={tick} map={map.uniqueID} previousTick={sample.Tick} attempts={sample.Attempts}"
            + $" rejected={sample.Rejected} requestedHeat={sample.Requested} addedHeat={sample.Added}");
        sample.Tick = tick;
        sample.Attempts = sample.Rejected = 0;
        sample.Requested = sample.Added = 0;
        Write(() => $"PROCESS tick={tick} map={map.uniqueID} space={map.IsSpace()} config={config?.defName ?? "null"}"
            + $" reason={(config == null ? "NO_CONFIG" : config.spaceOnly && !map.IsSpace() ? "NOT_SPACE" : "RUN")}"
            + $" refresh={config?.refreshIntervalTicks} ventInterval={config?.ventIntervalTicks} throttleStart={config?.throttleStartRatio}");
        Write(() => Snapshot(pipeMap, config));
        return true;
    }

    internal static void Injection(Map map, float requested, bool accepted, float delta)
    {
        Sample sample = Samples.GetValue(map, _ => new Sample());
        sample.Attempts++;
        if (!accepted) sample.Rejected++;
        sample.Requested += requested;
        sample.Added += delta;
    }

    private static string Fields(object value, params string[] names)
    {
        if (value == null) return "null";
        return string.Join(" ", names.Select(name => {
            FieldInfo field = AccessTools.Field(value.GetType(), name);
            return name + "=" + (field == null ? "<missing>" : field.GetValue(value)?.ToString() ?? "null");
        }));
    }

    private static string ThingState(ThingWithComps thing) => thing == null ? "null" :
        $"{thing.ThingID}@{thing.Position} spawned={thing.Spawned} destroyed={thing.Destroyed}"
        + $" flick={thing.TryGetComp<CompFlickable>()?.SwitchIsOn} power={thing.TryGetComp<CompPowerTrader>()?.PowerOn}"
        + $" breakdown={thing.TryGetComp<CompBreakdownable>()?.BrokenDown}";

    internal static bool ConsumerDue(ThingWithComps thing)
    {
        if (thing?.Map == null) return false;
        int tick = Find.TickManager?.TicksGame ?? 0;
        Sample sample = Consumers.GetValue(thing, _ => new Sample());
        if (sample.Tick != int.MinValue && tick >= sample.Tick && tick - sample.Tick < 300) return false;
        sample.Tick = tick;
        return true;
    }

    internal static void Consumer(ThingWithComps thing, string phase)
    {
        Write(() => {
            var sb = new StringBuilder($"CONSUMER_{phase} tick={Find.TickManager?.TicksGame} map={thing.Map?.uniqueID} {ThingState(thing)}");
            sb.Append(" " + Fields(thing, thing is Turbine
                ? new[] { "UncappedPowerGeneration", "powerOutput", "RPM", "UncooledWater" }
                : new[] { "RealTemp", "postReturnTemp", "coolingCapPct", "coolingCapPctTo", "overheating", "overheatingTarget", "IsShutdown" }));
            foreach (var pipe in thing.GetComps<CompPipe>())
            {
                sb.Append($" pipe={pipe.mode}:{Id(pipe.net)}");
                if (pipe.net is CoolingNet cooling)
                    sb.Append($" capacity={cooling.CoolingCapacity} ratio={cooling.CoolingLoopRatio}");
            }
            return sb.ToString();
        });
    }

    private static string Snapshot(UniversalPipeMapComp pipeMap, RimatomicsSOS2HeatBridgeDef config)
    {
        var sb = new StringBuilder($"SNAPSHOT map={pipeMap.map.uniqueID}");
        foreach (CoolingNet net in pipeMap.PipeNets?.OfType<CoolingNet>() ?? Enumerable.Empty<CoolingNet>())
        {
            sb.Append($"\n COOLING net={Id(net)} nativeCapacity={net.CoolingCapacity} nativeRatio={net.CoolingLoopRatio}"
                + $" coolers={net.Coolers?.Count} turbines={net.Turbines?.Count}");
            foreach (var cooler in net.Coolers ?? Enumerable.Empty<CoolingSystem>())
                sb.Append($"\n  COOLER {ThingState(cooler)} coolingWatts={cooler?.coolingCapacity} {Fields(cooler, "Stalled")}");
            foreach (var turbine in net.Turbines ?? Enumerable.Empty<Turbine>())
                sb.Append($"\n  TURBINE {ThingState(turbine)} {Fields(turbine, "UncappedPowerGeneration", "powerOutput", "RPM", "UncooledWater")}");
            foreach (var thing in net.PipedThings ?? Enumerable.Empty<ThingWithComps>())
                if (thing != null && config?.IsBridgeThing(thing.def) == true)
                    sb.Append($"\n  LINK {ThingState(thing)} heatComp={thing.TryGetComp<CompShipHeat>() != null}"
                        + $" heatNet={thing.TryGetComp<CompShipHeat>()?.myNet?.GridID}");
        }
        foreach (var reactor in pipeMap.map.listerThings.AllThings.OfType<reactorCore>())
            sb.Append($"\n REACTOR {ThingState(reactor)} {Fields(reactor, "RealTemp", "RealTempPct", "postReturnTemp", "coolingCapPct", "coolingCapPctTo", "ThermalEnergy", "overheating", "overheatingTarget", "IsShutdown", "RealControlRodPosition")}");
        return sb.ToString();
    }

    internal static string Sinks(ShipHeatNet net, RimatomicsSOS2HeatBridgeDef config)
    {
        var sb = new StringBuilder();
        foreach (var sink in net.Sinks ?? Enumerable.Empty<CompShipHeatSink>())
        {
            if (sink?.parent == null) { sb.Append("\n  SINK null"); continue; }
            bool mapped = config.TryGetCoolingWatts(sink.parent.def, out float watts);
            string reason = !sink.parent.Spawned || sink.parent.Destroyed ? "NOT_SPAWNED"
                : sink.myNet != net ? "WRONG_NET" : sink.Disabled ? "DISABLED"
                : sink.Props.heatVent <= 0 ? "NO_VENT" : !mapped ? "UNMAPPED" : "ELIGIBLE";
            sb.Append($"\n  SINK {ThingState(sink.parent)} reason={reason} mappedWatts={watts} vent={sink.Props.heatVent}");
        }
        return sb.ToString();
    }
}

[HarmonyPatch]
internal static class RimatomicsHeatConsumerDiagnostics
{
    private static bool Prepare() => ModsConfig.IsActive("dubwise.rimatomics") && ModsConfig.IsActive("kentington.saveourship2");
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Turbine), "Tick");
        yield return AccessTools.Method(typeof(reactorCore), "Tick");
    }
    [HarmonyPrefix]
    private static void Prefix(ThingWithComps __instance, out bool __state)
    {
        __state = RimatomicsSOS2BridgeDiagnostics.ConsumerDue(__instance);
        if (__state) RimatomicsSOS2BridgeDiagnostics.Consumer(__instance, "BEFORE");
    }
    [HarmonyPostfix]
    private static void Postfix(ThingWithComps __instance, bool __state)
    {
        if (__state) RimatomicsSOS2BridgeDiagnostics.Consumer(__instance, "AFTER");
    }
}
