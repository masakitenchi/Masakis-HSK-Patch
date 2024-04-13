namespace Core_SK_Patch;

public class HediffGiver_Fever : HediffGiver
{
    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        if (cause.TryGetComp<HediffComp_Immunizable>() == null) return;

    }

    private void AdjustFeverSeverity(ref float severity)
    {

    }
}

public class Hediff_Fever : HediffWithComps
{

    public override void Tick()
    {
        base.Tick();

    }

    private float CalculateTotalSeverity()
    {
        float sum = 0;
        foreach(var hediff in this.pawn.health.hediffSet.hediffs.Where(
            x => x.TryGetComp<HediffComp_Immunizable>() != null))
        {
            sum += hediff.Severity / hediff.def.lethalSeverity
        }
    }
}
