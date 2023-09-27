namespace Core_SK_Patch;

public class GameComponent_ResearchManager : GameComponent
{
    private Dictionary<ResearchProjectDef, int> _repeatCount;


    public void Notify_ResearchFinished(ResearchProjectDef def)
    {
        if (def.GetModExtension<ModExtension_RepeatableResearch>() is null) return;
        if (_repeatCount.TryGetValue(def, out var count))
            ++_repeatCount[def];
        else
            _repeatCount.TryAdd(def, 1);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref _repeatCount, "researchCounts", LookMode.Def, LookMode.Value);
    }

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        _repeatCount = new();
    }

}
