namespace Core_SK_Patch;

public class StatPart_Upgradeable : StatPart
{
    public List<ResearchProjectDef> Researches;

    public override void TransformValue(StatRequest req, ref float val)
    {
        throw new NotImplementedException();
    }
}
