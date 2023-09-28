using System.Text.RegularExpressions;

namespace Core_SK_Patch;


/*
 * 
 */
[HarmonyPatch]
public class GameComponent_ResearchManager : GameComponent
{
    //If it ends with +X (where X is a number)
    private static readonly Regex labelEndReg = new Regex(@"\+\d+");

    private Dictionary<ResearchProjectDef, int> _repeatCount = new();

    public GameComponent_ResearchManager(Game game)
    { 
    }

    public void Notify_ResearchFinished(ResearchProjectDef def)
    {
        var ext = def.GetModExtension<ModExtension_RepeatableResearch>();
        if (ext is null) return;
        int count;
        if (_repeatCount.TryGetValue(def, out count))
            ++_repeatCount[def];
        else
        {
            count = 1;
            _repeatCount.TryAdd(def, count);
        }
        //Will change the description & label accordingly some day
        //Somehow not working?
        /*if (!labelEndReg.Match(def.label).Value.NullOrEmpty())
            def.label.Replace(labelEndReg.Match(def.label).Value, $"+{count}");
        else
            def.label += $" +{count}";*/
        if(ext.MaxRepeatableCount > count)
            Find.ResearchManager.progress[def] = 0f;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref _repeatCount, "researchCounts", LookMode.Def, LookMode.Value);
        if(Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            foreach (var def in _repeatCount)
                for (var i = 0; i < def.Value; i++)
                    foreach (var mod in def.Key.researchMods)
                        mod.Apply();
        }
    }

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        _repeatCount = new();
    }


    //For back-compatiblity
    public override void FinalizeInit()
    {
        base.FinalizeInit();
        foreach(var def in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => x.GetModExtension<ModExtension_RepeatableResearch>() != null))
        {
            if (def.IsFinished && !this._repeatCount.TryGetValue(def, out _))
            {
                _repeatCount.Add(def, 1);
                Find.ResearchManager.progress[def] = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(ResearchManager),nameof(ResearchManager.FinishProject))]
    public static void Postfix(ResearchProjectDef proj)
    {
        Current.Game.GetComponent<GameComponent_ResearchManager>().Notify_ResearchFinished(proj);
    }
}
