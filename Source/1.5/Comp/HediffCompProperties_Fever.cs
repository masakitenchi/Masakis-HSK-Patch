namespace Core_SK_Patch;

public class HediffCompProperties_Fever : HediffCompProperties
{
    public HediffCompProperties_Fever() => this.compClass = typeof(HediffComp_Fever);
}

public class HediffComp_Fever : HediffComp
{
    public HediffCompProperties_Fever Props => this.props as HediffCompProperties_Fever;


    public override bool CompShouldRemove => !this.Pawn.health.hediffSet.hediffs.Any(x => x.def.HasComp(typeof(HediffComp_Immunizable)));

}
