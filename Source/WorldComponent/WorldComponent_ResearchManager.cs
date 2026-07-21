using System.Text.RegularExpressions;
using RimWorld.Planet;
using SK.Utils;

namespace Core_SK_Patch;

[HarmonyPatch]
[StaticConstructorOnStartup]
public class GameComponent_ResearchManager : WorldComponent
{
    private static readonly Dictionary<ResearchProjectDef, float> _initialCost = new();
    //If it ends with +X (where X is a number)
    private static readonly Regex labelEndReg = new Regex(@"\+\d+");

    private Dictionary<ResearchProjectDef, int> _repeatCount = new();

    static GameComponent_ResearchManager()
    {
        foreach(var def in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => x.HasModExtension<ModExtension_RepeatableResearch>()))
        {
            _initialCost.TryAdd(def, def.baseCost);
        }
    }
    public GameComponent_ResearchManager(World world) : base(world)
    {
    }

    public void Notify_ResearchFinished(ResearchProjectDef def)
    {
        var ext = def.GetModExtension<ModExtension_RepeatableResearch>();
        if (ext is null) return;
        var count = _repeatCount.TryGetValue(def, out var previousCount) ? previousCount + 1 : 1;
        if (ext.MaxRepeatableCount > 0)
            count = Math.Min(count, ext.MaxRepeatableCount);
        _repeatCount[def] = count;
        if (ext.MaxRepeatableCount > count)
            ResetProgressAndAdjustCost(def, count);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref _repeatCount, "researchCounts", LookMode.Def, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            _repeatCount ??= new Dictionary<ResearchProjectDef, int>();
            foreach (var entry in _repeatCount.ToList())
            {
                var def = entry.Key;
                var ext = def?.GetModExtension<ModExtension_RepeatableResearch>();
                if (ext is null) continue;

                var count = Math.Max(entry.Value, 0);
                if (ext.MaxRepeatableCount > 0 && count > ext.MaxRepeatableCount)
                {
                    Logger.Warning($"Clamping corrupted repeat count for {def.defName} from {count} to {ext.MaxRepeatableCount}.");
                    count = ext.MaxRepeatableCount;
                }
                _repeatCount[def] = count;

                ApplyResearchMods(def, count);
                ResearchScriber.RegisterAppliedMods(def);
                AdjustCostForRepeatCount(def, count);
            }
        }
    }


    //For back-compatiblity
    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);
        foreach (var def in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => x.HasModExtension<ModExtension_RepeatableResearch>()))
        {
            if (def.IsFinished && !this._repeatCount.TryGetValue(def, out _))
            {
                _repeatCount.Add(def, 1);
                ApplyResearchMods(def, 1);
                ResearchScriber.RegisterAppliedMods(def);
                ResetProgressAndAdjustCost(def, 1);
            }
        }
    }

    [ClearDataOnNewGame]
    private static void ResetBasecost()
    {
        foreach (var kvpair in _initialCost)
        {
            kvpair.Key.baseCost = kvpair.Value;
        }
    }
    private static void ApplyResearchMods(ResearchProjectDef def, int count)
    {
        if (def.researchMods is null) return;
        for (var i = 0; i < count; i++)
        {
            foreach (var mod in def.researchMods)
                mod.Apply();
        }
    }

    private static void AdjustCostForRepeatCount(ResearchProjectDef def, int count)
    {
        var ext = def.GetModExtension<ModExtension_RepeatableResearch>();
        if (ext is null || !_initialCost.TryGetValue(def, out var initialCost)) return;

        var costIncreases = count;
        if (ext.MaxRepeatableCount > 0)
            costIncreases = Math.Min(costIncreases, Math.Max(ext.MaxRepeatableCount - 1, 0));
        def.baseCost = initialCost * Mathf.Pow(ext.CostMultiplier, costIncreases);
    }

    private static void ResetProgressAndAdjustCost(ResearchProjectDef def, int count)
    {
        Find.ResearchManager.progress[def] = 0f;
        AdjustCostForRepeatCount(def, count);
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    [HarmonyPrefix]
    public static void Prefix(ResearchProjectDef proj)
    {
        if (proj.HasModExtension<ModExtension_RepeatableResearch>())
            ResearchScriber.AllowNextApplication(proj);
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    [HarmonyPostfix]
    public static void Postfix(ResearchProjectDef proj)
    {
        Current.Game.World.GetComponent<GameComponent_ResearchManager>().Notify_ResearchFinished(proj);
    }
}
