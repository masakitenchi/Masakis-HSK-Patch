namespace Core_SK_Patch;

public class ITab_Engine : ITab
{
    public CompEngine CompEngine;


    public override void FillTab()
    {
        Rect inRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(5f);
        Widgets.BeginGroup(inRect);

        Widgets.EndGroup();
    }

    public override void TabTick()
    {
        base.TabTick();

    }

}
