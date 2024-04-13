namespace Core_SK_Patch;

// Temp class for replace CompProperties_LowIdleDraw with CompProperties_Power.idlePowerDraw
// Vanilla only has code like this.PowerTraderComp.PowerOutput = this.Working ? -this.PowerComp.Props.PowerConsumption : -this.PowerComp.Props.idlePowerDraw;
// But not all Buildings have Working property. So it's more complicated that I expected. Temporarily halted.
/*internal class Transition_LowIdlePowerDraw
{
}
*/