using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;


//I could make it search through all sowable plants for a minimum fertility... but 40% should be good enough for most of them
[HarmonyPatch]
public static class Patch_GrowZoneMinFertility
{
	private static readonly MethodInfo BiotechActive = AccessTools.PropertyGetter(typeof(ModsConfig), nameof(ModsConfig.BiotechActive));

	[HarmonyPatch(typeof(Designator_ZoneAdd_Growing), nameof(Designator_ZoneAdd_Growing.CanDesignateCell))]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		Label? label = null;
		bool Brtrue = true;
		bool Active = false;
		foreach(var inst in instructions)
		{
			if(Brtrue && inst.Branches(out label))
			{
				Brtrue = false;
			}
			if (!Active && inst.Calls(BiotechActive))
			{
				Active = true;
				continue;
			}
			if (Active && inst.labels is not null)
			{
				inst.labels.Clear();	
			}
			if(Active && inst.Is(OpCodes.Ldc_R4, 0.5f))
			{
				Active = false;
				inst.operand = 0.4f;
				inst.labels.Add(label.Value);
			}
			if(!Active)
				yield return inst;
		}
	}
}
