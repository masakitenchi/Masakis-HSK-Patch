using RimWorld;

namespace Core_SK_Patch;

public class SpecialThingFilterWorker_MealSimple : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.IsIngestible;

    public override bool AlwaysMatches(ThingDef def) => def.thingCategories.Contains(ThingCategoryDefOf.FoodMeals) && def.ingestible?.preferability == FoodPreferability.MealSimple;
    public override bool Matches(Thing t) => CanEverMatch(t.def) && AlwaysMatches(t.def);
}

public class SpecialThingFilterWorker_MealFine : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.IsIngestible;

    public override bool AlwaysMatches(ThingDef def) => def.thingCategories.Contains(ThingCategoryDefOf.FoodMeals) && def.ingestible?.preferability == FoodPreferability.MealFine;
    public override bool Matches(Thing t) => CanEverMatch(t.def) && AlwaysMatches(t.def);
}

public class SpecialThingFilterWorker_MealLavish : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.IsIngestible;

    public override bool AlwaysMatches(ThingDef def) => def.thingCategories.Contains(ThingCategoryDefOf.FoodMeals) && def.ingestible?.preferability == FoodPreferability.MealLavish;
    public override bool Matches(Thing t) => CanEverMatch(t.def) && AlwaysMatches(t.def);
}

public class SpecialThingFilterWorker_PrimeMeat : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.race is not null;

    public override bool AlwaysMatches(ThingDef def) => def.race.meatDef.defName == "Meat_Muffalo";

    public override bool Matches(Thing t) => CanEverMatch(t.def) && AlwaysMatches(t.def);
}

public class SpecialThingFilterWorker_RawMeat : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.race is not null;

    public override bool AlwaysMatches(ThingDef def) => def.race.meatDef.defName == "Meat_Elephant";

    public override bool Matches(Thing t) => CanEverMatch(t.def) && AlwaysMatches(t.def);
}

/*public class SpecialThingFilterWorker_FishMeat : SpecialThingFilterWorker
{
    public override bool CanEverMatch(ThingDef def) => def.race is not null;

    public override bool AlwaysMatches(ThingDef def) => def.race.meatDef.defName == "Meat_Fish";

    public override bool Matches(Thing t) => AlwaysMatches(t.def);
}*/