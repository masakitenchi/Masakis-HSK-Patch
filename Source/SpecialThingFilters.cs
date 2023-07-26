namespace Core_SK_Patch;

public class SpecialThingFilterWorker_MealSimple : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.IsIngestible;

    public override bool Matches(Thing t)
    {
        return t.def.ingestible?.preferability == FoodPreferability.MealSimple;
    }
}

public class SpecialThingFilterWorker_MealFine : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.IsIngestible;

    public override bool Matches(Thing t)
    {
        return t.def.ingestible?.preferability == FoodPreferability.MealFine;
    }
}

public class SpecialThingFilterWorker_MealLavish : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.IsIngestible;

    public override bool Matches(Thing t)
    {
        return t.def.ingestible?.preferability == FoodPreferability.MealLavish;
    }
}