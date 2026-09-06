using System.Runtime.CompilerServices;
using Rimatomics;
using SaveOurShip2;

namespace Core_SK_Patch;

internal static class RimatomicsSOS2HeatBridgeRuntime
{
    internal const string BridgeDefName = "CoreSK_RimatomicsSOS2HeatBridge";

    private static readonly ConditionalWeakTable<Map, BridgeCache> Caches = new();

    internal static RimatomicsSOS2HeatBridgeDef Config =>
        DefDatabase<RimatomicsSOS2HeatBridgeDef>.GetNamedSilentFail(BridgeDefName);

    internal static void Process(UniversalPipeMapComp pipeMap)
    {
        RimatomicsSOS2HeatBridgeDef config = Config;
        Map map = pipeMap?.map;
        if (config == null || map == null || config.spaceOnly && !map.IsSpace())
            return;

        int ticksGame = Find.TickManager.TicksGame;
        BridgeCache cache = Caches.GetValue(map, _ => new BridgeCache());
        if (cache.ShouldRefresh(pipeMap, ticksGame, config.refreshIntervalTicks))
            cache.Rebuild(pipeMap, config, ticksGame);

        foreach (BridgeGroup group in cache.Groups)
            ProcessGroup(group, config);
    }

    internal static float GetAvailableCoolingWatts(ShipHeatNet heatNet)
    {
        RimatomicsSOS2HeatBridgeDef config = Config;
        if (config == null || heatNet == null)
            return 0f;
        return HeatNetState.Create(heatNet, config)?.AvailableCoolingWatts ?? 0f;
    }

    internal static bool HasSufficientBridgeCooling(CoolingSystem cooler)
    {
        RimatomicsSOS2HeatBridgeDef config = Config;
        Map map = cooler?.Map;
        if (config == null || map == null || config.spaceOnly && !map.IsSpace() ||
            !Caches.TryGetValue(map, out BridgeCache cache))
            return false;

        CoolingNet net = cooler.GetComps<CompPipe>()
            .FirstOrDefault(pipe => pipe.mode == PipeType.Cooling)?.net as CoolingNet;
        if (net == null)
            return false;

        float demand = net.Turbines?.Sum(turbine => Mathf.Max(0f, turbine.UncappedPowerGeneration)) ?? 0f;
        if (demand <= 0f || !(net.CoolingCapacity >= demand))
            return false;

        // Only suppress the individual-cooler warning while a live bridge is present.
        // A disconnected or full SOS2 network must not hide native cooling warnings.
        return cache.Groups.Any(group => group.CoolingNets.Contains(net) &&
            group.HeatNets.Any(heatNet =>
                group.TryGetConnectedInjector(heatNet, config, out _) &&
                (HeatNetState.Create(heatNet, config)?.AvailableCoolingWatts ?? 0f) > 0f));
    }

    private static void ProcessGroup(BridgeGroup group, RimatomicsSOS2HeatBridgeDef config)
    {
        List<HeatNetState> heatStates = group.HeatNets
            .Where(heatNet => group.TryGetConnectedInjector(heatNet, config, out _))
            .Select(heatNet => HeatNetState.Create(heatNet, config))
            .Where(state => state != null && state.AvailableCoolingWatts > 0f)
            .ToList();

        float totalBridgeCapacity = heatStates.Sum(state => state.AvailableCoolingWatts);
        List<CoolingNetState> coolingStates = group.CoolingNets
            .Select(net => CoolingNetState.Create(net, config.targetCoolingRatio))
            .Where(state => state != null)
            .ToList();

        float totalUnmetDemand = coolingStates.Sum(state => state.UnmetDemandWatts);
        float totalBridgeUsed = 0f;

        // Native Rimatomics coolers are used first. The shared SOS2 capacity is then
        // divided between loops to reach the target load ratio, including reserve capacity.
        foreach (CoolingNetState state in coolingStates)
        {
            float bridgeAllocation = 0f;
            if (totalBridgeCapacity > 0f && totalUnmetDemand > 0f && state.UnmetDemandWatts > 0f)
            {
                bridgeAllocation = Mathf.Min(
                    state.UnmetDemandWatts,
                    totalBridgeCapacity * state.UnmetDemandWatts / totalUnmetDemand);
            }

            float combinedCapacity = state.PhysicalCoolingWatts + bridgeAllocation;
            state.Net.CoolingCapacity = combinedCapacity;
            state.Net.CoolingLoopRatio = combinedCapacity > 1f
                ? state.DemandWatts / combinedCapacity
                : 0f;
            // Reserved capacity is not heat. Only transfer demand unmet by native coolers.
            float bridgeHeatWatts = Mathf.Min(bridgeAllocation,
                Mathf.Max(0f, state.DemandWatts - state.PhysicalCoolingWatts));
            totalBridgeUsed += bridgeHeatWatts;
        }

        if (totalBridgeUsed <= 0f || totalBridgeCapacity <= 0f)
            return;

        foreach (HeatNetState state in heatStates)
        {
            // A mapping contributes both Rimatomics watts and the corresponding
            // fraction of the sink's native SOS2 heatVent load. No heat is deleted.
            float coolingShare = totalBridgeUsed * state.AvailableCoolingWatts / totalBridgeCapacity;
            float loadRatio = Mathf.Clamp01(coolingShare / state.AvailableCoolingWatts);
            float heatToAdd = state.AvailableHeatPerTick * loadRatio;
            if (heatToAdd > 0f && group.TryGetConnectedInjector(state.Net, config, out CompShipHeat injector))
            {
                injector.AddHeatToNetwork(heatToAdd);
            }
        }
    }

    private sealed class BridgeCache
    {
        internal readonly List<BridgeGroup> Groups = new();

        private UniversalPipeMapComp pipeMap;
        private int lastRefreshTick = int.MinValue;

        internal bool ShouldRefresh(UniversalPipeMapComp currentPipeMap, int ticksGame, int refreshIntervalTicks)
        {
            return pipeMap != currentPipeMap || lastRefreshTick == int.MinValue ||
                ticksGame - lastRefreshTick >= refreshIntervalTicks;
        }

        internal void Rebuild(
            UniversalPipeMapComp currentPipeMap,
            RimatomicsSOS2HeatBridgeDef config,
            int ticksGame)
        {
            pipeMap = currentPipeMap;
            lastRefreshTick = ticksGame;
            Groups.Clear();

            List<CoolingNet> coolingNets = currentPipeMap.PipeNets?
                .OfType<CoolingNet>()
                .Where(net => net != null)
                .ToList() ?? new List<CoolingNet>();

            Dictionary<CoolingNet, HashSet<ShipHeatNet>> coolingToHeat = new();
            Dictionary<ShipHeatNet, HashSet<CoolingNet>> heatToCooling = new();
            Dictionary<ShipHeatNet, CompShipHeat> injectors = new();

            foreach (CoolingNet coolingNet in coolingNets)
            {
                foreach (ThingWithComps thing in coolingNet.PipedThings ?? Enumerable.Empty<ThingWithComps>())
                {
                    if (thing == null || !config.IsBridgeThing(thing.def))
                        continue;

                    CompShipHeat heatComp = thing.TryGetComp<CompShipHeat>();
                    ShipHeatNet heatNet = heatComp?.myNet;
                    if (heatNet == null)
                        continue;

                    if (!coolingToHeat.TryGetValue(coolingNet, out HashSet<ShipHeatNet> heatNets))
                        coolingToHeat[coolingNet] = heatNets = new HashSet<ShipHeatNet>();
                    if (!heatToCooling.TryGetValue(heatNet, out HashSet<CoolingNet> attachedCoolingNets))
                        heatToCooling[heatNet] = attachedCoolingNets = new HashSet<CoolingNet>();

                    heatNets.Add(heatNet);
                    attachedCoolingNets.Add(coolingNet);
                    injectors[heatNet] = heatComp;
                }
            }

            HashSet<CoolingNet> visitedCooling = new();
            HashSet<ShipHeatNet> visitedHeat = new();
            foreach (CoolingNet start in coolingToHeat.Keys)
            {
                if (!visitedCooling.Add(start))
                    continue;

                BridgeGroup group = new();
                Queue<object> queue = new();
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    object current = queue.Dequeue();
                    if (current is CoolingNet coolingNet)
                    {
                        group.CoolingNets.Add(coolingNet);
                        if (!coolingToHeat.TryGetValue(coolingNet, out HashSet<ShipHeatNet> heatNets))
                            continue;
                        foreach (ShipHeatNet heatNet in heatNets)
                        {
                            if (visitedHeat.Add(heatNet))
                                queue.Enqueue(heatNet);
                        }
                    }
                    else if (current is ShipHeatNet heatNet)
                    {
                        group.HeatNets.Add(heatNet);
                        group.Injectors[heatNet] = injectors[heatNet];
                        if (!heatToCooling.TryGetValue(heatNet, out HashSet<CoolingNet> attachedCoolingNets))
                            continue;
                        foreach (CoolingNet attachedCoolingNet in attachedCoolingNets)
                        {
                            if (visitedCooling.Add(attachedCoolingNet))
                                queue.Enqueue(attachedCoolingNet);
                        }
                    }
                }

                if (group.CoolingNets.Count > 0 && group.HeatNets.Count > 0)
                    Groups.Add(group);
            }
        }
    }

    private sealed class BridgeGroup
    {
        internal readonly List<CoolingNet> CoolingNets = new();
        internal readonly List<ShipHeatNet> HeatNets = new();
        internal readonly Dictionary<ShipHeatNet, CompShipHeat> Injectors = new();

        internal bool TryGetConnectedInjector(
            ShipHeatNet heatNet,
            RimatomicsSOS2HeatBridgeDef config,
            out CompShipHeat injector)
        {
            if (!Injectors.TryGetValue(heatNet, out injector) || injector?.parent == null ||
                !injector.parent.Spawned || injector.myNet != heatNet ||
                !config.IsBridgeThing(injector.parent.def))
                return false;

            return injector.parent.GetComps<CompPipe>().Any(pipe =>
                pipe.mode == PipeType.Cooling && pipe.net is CoolingNet coolingNet &&
                CoolingNets.Contains(coolingNet));
        }
    }

    private sealed class CoolingNetState
    {
        internal CoolingNet Net;
        internal float DemandWatts;
        internal float PhysicalCoolingWatts;
        internal float UnmetDemandWatts;

        internal static CoolingNetState Create(CoolingNet net, float targetCoolingRatio)
        {
            if (net == null)
                return null;

            float demand = net.Turbines?.Sum(turbine => Mathf.Max(0f, turbine.UncappedPowerGeneration)) ?? 0f;
            float physicalCooling = net.Coolers?.Sum(cooler => Mathf.Max(0f, cooler.coolingCapacity)) ?? 0f;
            float targetRatio = targetCoolingRatio > 0f && targetCoolingRatio <= 1f
                ? targetCoolingRatio : 0.9f;
            return new CoolingNetState
            {
                Net = net,
                DemandWatts = demand,
                PhysicalCoolingWatts = physicalCooling,
                UnmetDemandWatts = Mathf.Max(0f, demand / targetRatio - physicalCooling)
            };
        }
    }

    private sealed class HeatNetState
    {
        internal ShipHeatNet Net;
        internal float AvailableCoolingWatts;
        internal float AvailableHeatPerTick;

        internal static HeatNetState Create(ShipHeatNet net, RimatomicsSOS2HeatBridgeDef config)
        {
            if (net == null || net.StorageCapacity <= 0f)
                return null;

            float nominalCoolingWatts = 0f;
            float nominalHeatPerTick = 0f;
            foreach (CompShipHeatSink sink in net.Sinks ?? Enumerable.Empty<CompShipHeatSink>())
            {
                if (sink?.parent == null || !sink.parent.Spawned || sink.parent.Destroyed ||
                    sink.myNet != net || sink.Disabled || sink.Props.heatVent <= 0f ||
                    !config.TryGetCoolingWatts(sink.parent.def, out float coolingWatts))
                    continue;

                nominalCoolingWatts += coolingWatts;
                nominalHeatPerTick += sink.Props.heatVent / config.ventIntervalTicks;
            }

            if (nominalCoolingWatts <= 0f || nominalHeatPerTick <= 0f)
                return null;

            float fillRatio = Mathf.Clamp01(net.StorageUsed / net.StorageCapacity);
            float throttle = fillRatio <= config.throttleStartRatio
                ? 1f
                : Mathf.Clamp01((1f - fillRatio) / (1f - config.throttleStartRatio));
            float acceptance = Mathf.Clamp01(net.StorageAvailable / nominalHeatPerTick);
            float availableRatio = Mathf.Min(throttle, acceptance);

            return new HeatNetState
            {
                Net = net,
                AvailableCoolingWatts = nominalCoolingWatts * availableRatio,
                AvailableHeatPerTick = nominalHeatPerTick * availableRatio
            };
        }
    }
}
