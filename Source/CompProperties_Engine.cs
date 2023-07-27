using SK;

namespace Core_SK_Patch;

public class CompProperties_Engine : CompProperties
{
    public List<ThingDef> EnginesCanUse;

    public CompProperties_Engine() => this.compClass = typeof(CompEngine);
}

public class CompEngine : ThingComp
{
    private CompPowerTrader compPowerTrader;
    public CompProperties_Engine Props => this.props as CompProperties_Engine;
    private ThingDef _currentEngine;
    private ThingDef _EngineStuff;
    public float SpeedOffset => this._currentEngine?.GetModExtension<EngineModExtension>().EngineEfficiency ?? 0f;
    public float PowerMultiplier => this._currentEngine?.GetModExtension<EngineModExtension>().PowerConsumptionMultiplier ?? 1f;
    public ThingDef CurrentEngine => _currentEngine;

    public bool HasEngine => this._currentEngine != null;

    public void SetEngine(ThingDef engine, ThingDef stuff = null)
    {
        if (_currentEngine != null)
        {
            GenSpawn.Spawn(ThingMaker.MakeThing(this._currentEngine, stuff), parent.InteractionCell, parent.MapHeld);

        }
        Find.CurrentMap.listerThings.ThingsOfDef(engine).RandomElement().DeSpawn();
        _currentEngine = engine;
        _EngineStuff = stuff;
        Notify_EngineChanged();
    }
    public void RemoveEngine()
    {
        GenSpawn.Spawn(ThingMaker.MakeThing(this._currentEngine, _EngineStuff), parent.InteractionCell, parent.MapHeld);
        _currentEngine = null;
        _EngineStuff = null;
        Notify_EngineChanged();
    }
    public void Notify_EngineChanged()
    {
        compPowerTrader.SetUpPowerVars();
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        this.compPowerTrader = parent.TryGetComp<CompPowerTrader>();
        compPowerTrader?.SetUpPowerVars();
    }
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (this.compPowerTrader is not null && this.HasEngine)
        {
            //Log.Message($"Before PostSpawnSetup:{compPowerTrader.powerOutputInt}");
            Notify_EngineChanged();
            //Log.Message($"After PostSpawnSetup:{compPowerTrader.powerOutputInt}");
        }
    }
    public override void PostExposeData()
    {
        Scribe_Defs.Look(ref _currentEngine, "Engine");
        Scribe_Defs.Look(ref _EngineStuff, "EngineStuff");
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (DebugSettings.godMode)
        {
            yield return new Command_Action()
            {
                defaultDesc = "DEV_SETEngine",
                defaultLabel = "DEV_SETEngine",
                icon = HasEngine ? _currentEngine.uiIcon : BaseContent.BadTex,
                action = () => FloatMenuUtility.MakeMenu(Props.EnginesCanUse, (x) => x.label, x => () =>
                {
                    SetEngine(x);
                }
                )
            };
        }
        else
        {
            yield return new Command_Action()
            {
                defaultDesc = "AddOrChangeEngineDesc".Translate(),
                defaultLabel = HasEngine? _currentEngine.label : "AddEngine".Translate(),
                icon = HasEngine ? _currentEngine.uiIcon : BaseContent.BadTex,
                action = () =>
                {
                    Find.WindowStack.Add(new FloatMenu(GetAvailableEngines().ToList()));
                }
            };
        }
        if (HasEngine)
            yield return new Command_Action()
            {
                defaultDesc = "RemoveEngineDesc".Translate(),
                defaultLabel = "RemoveEngine".Translate(),
                action = RemoveEngine
            };
    }
    private IEnumerable<FloatMenuOption> GetAvailableEngines()
    {
        for (var i = 0; i < Props.EnginesCanUse.Count; i++)
        {
            ThingDef def = Props.EnginesCanUse[i];
            FloatMenuOption option = new FloatMenuOption(def.LabelCap, () => this.SetEngine(def));
            if (!parent.MapHeld.listerThings.AllThings.Any(x => x.def == Props.EnginesCanUse[i]))
            {
                option.Label += "NoAvailableEngine".Translate();
                option.action = null;
            }
            yield return option;
        }
    }
}

[HarmonyPatch]
public static class EnginePatch
{
    [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.ConsumeFuel))]
    [HarmonyPrefix]
    public static bool AdjustConsumptionRate(ref float amount, CompRefuelable __instance)
    {
        var comp = __instance.parent.TryGetComp<CompEngine>();
        if (comp is not null && comp.HasEngine)
        {
            amount *= comp.PowerMultiplier;
        }
        return true;
    }

    [HarmonyPatch(typeof(CompPowerTrader), nameof(CompPowerTrader.PowerOutput), MethodType.Setter)]
    [HarmonyPostfix]
    public static void AdjustPowerConsumption(ref float value, CompPowerTrader __instance)
    {
        if (__instance.parent.TryGetComp<CompEngine>() == null || !__instance.parent.TryGetComp<CompEngine>().HasEngine) return;
        value *= __instance.parent.TryGetComp<CompEngine>().PowerMultiplier;
    }
}