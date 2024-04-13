/*namespace Core_SK_Patch;

public class StatPart_MechBuff: StatPart
{
    public bool Applies(Thing t) => t.def.race?.IsMechanoid ?? false;

    public override string ExplanationPart(StatRequest req)
    {
        throw new NotImplementedException();
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.HasThing || !Applies(req.Thing)) return;
        val += 
    }
}*/
