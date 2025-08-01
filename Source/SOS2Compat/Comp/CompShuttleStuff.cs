namespace Core_SK_Patch;

public class CompShuttleStuff : ThingComp
{
    public ThingDef stuff;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref stuff, "stuff");
    }
}
