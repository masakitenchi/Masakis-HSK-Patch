using RimWorld.Planet;

namespace Core_SK_Patch;

[HarmonyPatch(typeof(GameComponentUtility), nameof(GameComponentUtility.LoadedGame))]
public static class FactionDiscovery_LoadedGame_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        // VEF contains the original implementation; do not open two competing dialogs
        // while it is still present in a transition mod list.
        if (Settings.EnableFactionDiscovery
            && !ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core"))
            LongEventHandler.ExecuteWhenFinished(() => FactionDiscoveryUtility.ScanAndPrompt());
    }
}

[HarmonyPatch(typeof(GameComponentUtility), nameof(GameComponentUtility.StartedNewGame))]
public static class FactionDiscovery_StartedNewGame_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
            Find.World?.GetComponent<FactionDiscoveryState>()?.Ignore(DefDatabase<FactionDef>.AllDefs));
    }
}

public sealed class FactionDiscoveryState : WorldComponent
{
    private HashSet<FactionDef> ignoredFactions = new();

    public FactionDiscoveryState(World world) : base(world) { }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref ignoredFactions, "CoreSK_ignoredFactionDefs", LookMode.Def);
        ignoredFactions ??= new HashSet<FactionDef>();
    }

    public bool IsIgnored(FactionDef def) => ignoredFactions.Contains(def);
    public void Ignore(FactionDef def) => ignoredFactions.Add(def);
    public void Ignore(IEnumerable<FactionDef> defs) => ignoredFactions.AddRange(defs);
    public void Clear() => ignoredFactions.Clear();
}

public static class FactionDiscoveryUtility
{
    private const float SettlementsPer100Ktiles = 80f;
    private const float NewFactionSettlementFactor = 0.7f;

    public static void ScanAndPrompt(bool clearIgnored = false)
    {
        if (Current.Game == null || Find.World == null || Find.FactionManager == null)
            return;

        var state = Find.World.GetComponent<FactionDiscoveryState>();
        if (clearIgnored)
            state.Clear();

        List<FactionDef> missing = DefDatabase<FactionDef>.AllDefsListForReading
            .Where(IsEligible)
            .Where(def => Find.FactionManager.AllFactionsListForReading.All(faction => faction.def != def))
            .Where(def => !state.IsIgnored(def))
            .ToList();

        if (missing.Count == 0)
        {
            if (clearIgnored)
                Messages.Message("CoreSK_FactionDiscovery_None".Translate(), MessageTypeDefOf.NeutralEvent, false);
            return;
        }

        Find.WindowStack.Add(new Dialog_FactionDiscovery(missing));
    }

    private static bool IsEligible(FactionDef def)
    {
        return def != null
            && !def.isPlayer
            && !def.hidden
            && def.requiredCountAtGameStart > 0
            && def.defName != "PColony";
    }

    public static int RecommendedSettlementCount()
    {
        int visibleFactionCount = Mathf.Max(1, Find.FactionManager.AllFactionsVisible.Count());
        return Mathf.Max(1, GenMath.RoundRandom(
            Find.WorldGrid.TilesCount / 100000f
            * SettlementsPer100Ktiles
            / visibleFactionCount
            * NewFactionSettlementFactor));
    }

    public static Faction Spawn(FactionDef def, int settlementCount, int minimumDistance, out int spawned)
    {
        bool wasHidden = def.hidden;
        Faction faction;
        try
        {
            def.hidden = true;
            faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
        }
        finally
        {
            def.hidden = wasHidden;
        }

        Find.FactionManager.Add(faction);
        spawned = CreateSettlements(faction, settlementCount, minimumDistance);

        if (spawned == 0)
            faction.defeated = true;

        return faction;
    }

    private static int CreateSettlements(Faction faction, int amount, int minimumDistance)
    {
        int spawned = 0;
        HashSet<PlanetTile> attemptedTiles = new();

        for (int attempt = 0; attempt < amount * 4 && spawned < amount; attempt++)
        {
            PlanetTile tile = TileFinder.RandomSettlementTileFor(faction);
            if (!tile.Valid || !attemptedTiles.Add(tile) || !FarEnoughFromPlayer(tile, minimumDistance))
                continue;

            var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            settlement.SetFaction(faction);
            settlement.Tile = tile;
            settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
            Find.WorldObjects.Add(settlement);
            spawned++;
        }

        return spawned;
    }

    private static bool FarEnoughFromPlayer(PlanetTile tile, int minimumDistance)
    {
        foreach (Settlement settlement in Find.WorldObjects.SettlementBases)
        {
            if (settlement.Faction != Faction.OfPlayer)
                continue;

            if (Find.WorldGrid.TraversalDistanceBetween(tile, settlement.Tile, false, minimumDistance) < minimumDistance)
                return false;
        }

        return !Find.WorldObjects.ObjectsAt(tile).Any();
    }
}

public sealed class Dialog_FactionDiscovery : Window
{
    private readonly List<FactionDef> missing;
    private int index;
    private int settlementCount;
    private int minimumDistance;

    private FactionDef CurrentDef => missing[index];

    public override Vector2 InitialSize => new(620f, 420f);

    public Dialog_FactionDiscovery(List<FactionDef> missing)
    {
        this.missing = missing;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        settlementCount = FactionDiscoveryUtility.RecommendedSettlementCount();
        minimumDistance = SettlementProximityGoodwillUtility.MaxDist;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        Text.Font = GameFont.Medium;
        listing.Label("CoreSK_FactionDiscovery_Title".Translate(CurrentDef.LabelCap));
        Text.Font = GameFont.Small;
        listing.GapLine();
        listing.Label("CoreSK_FactionDiscovery_Source".Translate(CurrentDef.modContentPack?.Name ?? "Unknown"));
        listing.Label("CoreSK_FactionDiscovery_Description".Translate());
        listing.Gap();

        listing.Label("CoreSK_FactionDiscovery_Count".Translate(settlementCount));
        settlementCount = Mathf.RoundToInt(listing.Slider(settlementCount, 1, Mathf.Max(10, FactionDiscoveryUtility.RecommendedSettlementCount() * 4)));
        listing.Label("CoreSK_FactionDiscovery_Distance".Translate(minimumDistance));
        minimumDistance = Mathf.RoundToInt(listing.Slider(minimumDistance, 1, SettlementProximityGoodwillUtility.MaxDist * 2));
        listing.Gap();

        if (listing.ButtonText("CoreSK_FactionDiscovery_Add".Translate()))
            AddFaction();
        if (listing.ButtonText("CoreSK_FactionDiscovery_Skip".Translate()))
            Next();
        if (listing.ButtonText("CoreSK_FactionDiscovery_Ignore".Translate()))
        {
            Find.World.GetComponent<FactionDiscoveryState>().Ignore(CurrentDef);
            Next();
        }

        listing.End();
    }

    private void AddFaction()
    {
        try
        {
            Faction faction = FactionDiscoveryUtility.Spawn(CurrentDef, settlementCount, minimumDistance, out int spawned);
            if (spawned > 0)
                Messages.Message("CoreSK_FactionDiscovery_Success".Translate(faction.Name, spawned), MessageTypeDefOf.TaskCompletion, false);
            else
                Messages.Message("CoreSK_FactionDiscovery_Failed".Translate(CurrentDef.LabelCap), MessageTypeDefOf.RejectInput, false);
        }
        catch (Exception exception)
        {
            Log.Error($"[Core SK Patch] Failed to add faction {CurrentDef.defName}:\n{exception}");
            Messages.Message("CoreSK_FactionDiscovery_Failed".Translate(CurrentDef.LabelCap), MessageTypeDefOf.RejectInput, false);
        }

        Next();
    }

    private void Next()
    {
        index++;
        if (index >= missing.Count)
            Close();
    }
}
