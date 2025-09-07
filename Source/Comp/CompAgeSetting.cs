namespace Core_SK_Patch;

public class CompAgeSetting : ThingComp
{
    public const float MinAge = 0f;
    public const float MaxAge = 18f;
    private Building_GrowthVat vat;
    public float vatUntil = MaxAge;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        vat = this.parent as Building_GrowthVat;
        if (vat == null)
        {
            Log.Error("CompAgeSetting can only be added to Building_GrowthVat.");
            this.parent.comps.Remove(this);
        }
    }
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref vatUntil, "vatUntil", (int)MaxAge);
    }

    public void SetAge(int age)
    {
        vatUntil = Mathf.Clamp(age, MinAge, MaxAge);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (Find.Selector.SelectedObjects.Count == 1)
        {
            yield return new Gizmo_SetAge(this);
        }
        else
        {
            yield return new Command_SetAge(this)
            {
                defaultLabel = "Set Age",
                defaultDesc = "Set the age this creature will be grown to.",
            };
        }
    }

    public override void CompTickInterval(int delta)
    {
        if (vat.SelectedPawn is Pawn pawn && pawn.ageTracker.AgeBiologicalYears >= vatUntil)
        {
            Messages.Message("OccupantEjectedFromGrowthVat".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NeutralEvent);
            vat.FinishPawn();
        }
    }
}

public class Command_SetAge : Command
{
    public CompAgeSetting target;

    public Command_SetAge(CompAgeSetting target)
    {
        this.target = target;
    }
    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        Dialog_Slider slider = new Dialog_Slider("Set age to:", 0, 18, (x) => this.target.SetAge(x), (int)this.target.vatUntil);
        Find.WindowStack.Add(slider);
    }
}

public class Gizmo_SetAge : Gizmo_Slider
{
    public CompAgeSetting comp;
    public override float Target
    {
        get => this.comp.vatUntil / CompAgeSetting.MaxAge;
        set => this.comp.SetAge(Mathf.RoundToInt(value * CompAgeSetting.MaxAge));
    }

    public override bool DraggingBar { get; set; }
    public override bool IsDraggable => true;
    public override string BarLabel => "CSP_Age".Translate((int)this.comp.vatUntil);
    public override string GetTooltip() => "";

    public override float ValuePercent => comp.vatUntil / CompAgeSetting.MaxAge;

    public override string Title => "CSP_IncubateTo".Translate();

    public Gizmo_SetAge(CompAgeSetting target)
    {
        this.comp = target;
    }
}

