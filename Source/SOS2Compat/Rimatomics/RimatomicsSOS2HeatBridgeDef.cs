using SaveOurShip2;

namespace Core_SK_Patch;

public sealed class RimatomicsSOS2HeatBridgeDef : Def
{
    public List<ThingDef> bridgeThingDefs = new();
    public List<RimatomicsSOS2SinkMapping> sinkMappings = new();
    public float throttleStartRatio = 0.8f;
    // Legacy XML compatibility only. Capacity now follows hardware, not a target load ratio.
    public float targetCoolingRatio = 0.9f;
    public int ventIntervalTicks = 120;
    public int refreshIntervalTicks = 60;
    public bool spaceOnly = true;

    private Dictionary<ThingDef, float> coolingWattsByDef = new();

    public bool IsBridgeThing(ThingDef thingDef)
    {
        return thingDef != null && bridgeThingDefs.Contains(thingDef);
    }

    public bool TryGetCoolingWatts(ThingDef thingDef, out float coolingWatts)
    {
        return coolingWattsByDef.TryGetValue(thingDef, out coolingWatts);
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        coolingWattsByDef = (sinkMappings ?? new List<RimatomicsSOS2SinkMapping>())
            .Where(mapping => mapping?.thingDef != null && mapping.coolingWatts > 0f)
            .GroupBy(mapping => mapping.thingDef)
            .ToDictionary(group => group.Key, group => group.Last().coolingWatts);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
            yield return error;

        if (bridgeThingDefs.NullOrEmpty())
            yield return $"{defName} has no bridgeThingDefs";
        if (sinkMappings.NullOrEmpty())
            yield return $"{defName} has no sinkMappings";
        if (throttleStartRatio < 0f || throttleStartRatio >= 1f)
            yield return $"{defName} throttleStartRatio must be in [0, 1)";
        if (!(targetCoolingRatio > 0f && targetCoolingRatio <= 1f))
            yield return $"{defName} targetCoolingRatio must be in (0, 1]";
        if (ventIntervalTicks <= 0)
            yield return $"{defName} ventIntervalTicks must be positive";
        if (refreshIntervalTicks <= 0)
            yield return $"{defName} refreshIntervalTicks must be positive";

        foreach (RimatomicsSOS2SinkMapping mapping in sinkMappings ?? Enumerable.Empty<RimatomicsSOS2SinkMapping>())
        {
            if (mapping?.thingDef == null)
                yield return $"{defName} has a sink mapping without a thingDef";
            else if (mapping.coolingWatts <= 0f)
                yield return $"{defName} maps {mapping.thingDef.defName} to a non-positive cooling value";
        }
    }
}

public sealed class RimatomicsSOS2SinkMapping
{
    public ThingDef thingDef;
    public float coolingWatts;
}
