namespace Core_SK_Patch;

public class ResearchMod_ExpandOptimalTempRange : ResearchMod
{
    public override void Apply()
    {
        /*DefDatabase<ThingDef>.AllDefs.Where(x => x.IsBuildingArtificial && !x.IsFrame && x.selectable && x.useHitPoints && x.statBases is not null).Do(x =>
        {
            if (x.statBases.Exists(t => t.stat == minTemp)) x.statBases.First(t => t.stat == minTemp).value -= 1;
            else x.SetStatBaseValue(minTemp, minTemp.defaultBaseValue - 1);
            if (x.statBases.Exists(t => t.stat == maxTemp)) x.statBases.First(t => t.stat == maxTemp).value += 1;
            else x.SetStatBaseValue(maxTemp, maxTemp.defaultBaseValue + 1);
        }); */
        AccessTools.StaticFieldRefAccess<float>("Celsius.Setup:globalMinTempOffset") -= 1f;
        AccessTools.StaticFieldRefAccess<float>("Celsius.Setup:globalMaxTempOffset") += 1f;
    }
}
