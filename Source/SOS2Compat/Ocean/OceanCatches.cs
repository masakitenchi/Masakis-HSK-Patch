namespace Core_SK_Patch;

internal static class OceanCatches
{
    // Match Odyssey's per-map cooldown. Using its tracker also shares the
    // cooldown with natural fishing, multiple oceans and all four fishers.
    internal const int RareCooldownTicks = 300000;

    internal static bool RareEligible(int now, int lastCatch, bool debugOverride) =>
        debugOverride || lastCatch == 0 || (long)now - lastCatch > RareCooldownTicks;

    internal static ThingDef SelectFish(List<ThingDef> fallback)
    {
        // Build from live biome Defs so patches to the fish tables are respected.
        // Deduplicate: a fish appearing in many biomes must not get extra weight.
        bool uncommon = Rand.Chance(FishingUtility.ChanceToCatchUncommonFish);
        var fish = DefDatabase<BiomeDef>.AllDefsListForReading
            .Where(biome => biome.fishTypes != null)
            .SelectMany(biome => (uncommon ? biome.fishTypes.saltwater_Uncommon : biome.fishTypes.saltwater_Common)
                ?? Enumerable.Empty<FishChance>())
            .Where(entry => entry?.fishDef != null && entry.chance > 0f)
            .Select(entry => entry.fishDef)
            .Where(def => def.category == ThingCategory.Item && def.ingestible != null)
            .Distinct().ToList();
        if (!uncommon) fish = fish.Concat(fallback).Distinct().ToList();
        return fish.Count > 0 ? fish.RandomElement() : fallback.RandomElement();
    }

    // True means a rare batch was generated, even if the floor had no room.
    // Do not turn failed rare placement into another fish/reward roll.
    internal static bool TryRareCatch(Pawn pawn, out bool placed)
    {
        placed = false;
        var tracker = pawn.Map.waterBodyTracker;
        if (tracker == null || !RareEligible(Find.TickManager.TicksGame, tracker.lastRareCatchTick,
                DebugSettings.alwaysRareCatches)
            || !Rand.Chance(FishingUtility.ChanceForRareCatch)) return false;

        var pools = DefDatabase<BiomeDef>.AllDefsListForReading
            .Select(biome => biome.fishTypes?.rareCatchesSetMaker)
            .Where(pool => pool?.root != null).Distinct().ToList();
        if (pools.Count == 0) return false;
        // Invoke the original generators rather than copying their loot tables.
        // No fake WaterBody is registered on the ship floor.
        List<Thing> catches = pools.RandomElement().root.Generate();
        if (catches == null || catches.Count == 0) return false;

        var labels = new List<string>();
        var targets = new List<Thing>();
        foreach (Thing thing in catches)
        {
            if (thing == null) continue;
            string label = thing.LabelCap;
            int count = 0;
            bool allPlaced = GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near,
                placedAction: (actual, amount) =>
                {
                    count += amount;
                    if (!targets.Contains(actual)) targets.Add(actual);
                });
            if (count > 0) labels.Add(label);
            if (!allPlaced && !thing.Destroyed && !thing.Spawned) thing.Destroy();
        }
        placed = targets.Count > 0;
        if (placed)
        {
            tracker.lastRareCatchTick = Find.TickManager.TicksGame;
            Find.LetterStack.ReceiveLetter("LetterLabelRareCatch".Translate(),
                "LetterTextRareCatch".Translate(pawn.Named("PAWN")) + ":\n" + labels.ToLineList("  - "),
                LetterDefOf.PositiveEvent, targets);
        }
        // As in vanilla, rare items do not consume fish population and do not
        // count as slaughtering fish. Normal catches still debit actual placement.
        return true;
    }
}
