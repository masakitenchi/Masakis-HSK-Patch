

namespace Core_SK_Patch;

public class CompAutoForbidden : ThingComp
{
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if(!this.parent.MapHeld.areaManager.Home.ActiveCells.Contains(this.parent.PositionHeld))
            this.parent.SetForbidden(true);
    }

}
