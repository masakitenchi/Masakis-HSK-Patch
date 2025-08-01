namespace Core_SK_Patch;

public class Building_VoidTrashCan : Building_Crate
{

    public override void TickLong()
    {
        this.innerContainer.ClearAndDestroyContents();
    }
}


public class IngredientValueGetter_Mass : IngredientValueGetter
{
    public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing)
    {
        throw new NotImplementedException();
    }

    public override float ValuePerUnitOf(ThingDef t)
    {
        return t.statBases.Find(x => x.stat == StatDefOf.Mass).value;   
    }
}
