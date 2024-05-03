namespace Core_SK_Patch;

public class CompReport : ThingComp
{

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new Command_Action()
        {
            defaultLabel = "Report Is ColonyMech",
            action = () => Logger.Message($"{(this.parent as Pawn).IsColonyMech}")
        };
        yield return new Command_Action()
        {
            defaultLabel = "Report ShowDraftGizmo",
            action = () => Logger.Message($"{(this.parent as Pawn).drafter?.ShowDraftGizmo}")
        };
    }
}
