namespace Core_SK_Patch;

public class StatPart_Engine : StatPart
{
    public bool Applies(Thing t) => t.Spawned && (t.TryGetComp<CompEngine>()?.HasEngine ?? false);
    public override string ExplanationPart(StatRequest req)
    {
        if (req.HasThing && Applies(req.Thing))
        {
            ThingDef def = req.Thing.TryGetComp<CompEngine>()?.CurrentEngine;
            return def.LabelCap + " : x" + def.GetModExtension<EngineModExtension>().EngineEfficiency.ToString("P2");
        }
        else if (req.Thing.def.HasComp(typeof(CompEngine)))
        {
            return "NoEngine".Translate() + " : x " + 1.ToString("P0");
        }
        else
        {
            return "";
        }
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (!req.HasThing || !Applies(req.Thing)) return;
        val *= req.Thing.TryGetComp<CompEngine>().SpeedOffset;
    }
}
