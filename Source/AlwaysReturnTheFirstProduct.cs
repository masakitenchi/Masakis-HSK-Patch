
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

/*
 * // if (specialProducts != null)
	IL_0000: ldarg.0
	IL_0001: ldfld class [mscorlib]System.Collections.Generic.List`1<valuetype Verse.SpecialProductType> Verse.RecipeDef::specialProducts
	IL_0006: brfalse.s IL_000a

	// return null;
	IL_0008: ldnull
	IL_0009: ret

	// if (products == null || products.Count != 1)
	IL_000a: ldarg.0
	IL_000b: ldfld class [mscorlib]System.Collections.Generic.List`1<class Verse.ThingDefCountClass> Verse.RecipeDef::products
	IL_0010: brfalse.s IL_0020

	IL_0012: ldarg.0
	IL_0013: ldfld class [mscorlib]System.Collections.Generic.List`1<class Verse.ThingDefCountClass> Verse.RecipeDef::products
	IL_0018: callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<class Verse.ThingDefCountClass>::get_Count()
	IL_001d: ldc.i4.1
	IL_001e: beq.s IL_0022

	// return null;
	IL_0020: ldnull
	IL_0021: ret

	// return products[0].thingDef;
	IL_0022: ldarg.0
	IL_0023: ldfld class [mscorlib]System.Collections.Generic.List`1<class Verse.ThingDefCountClass> Verse.RecipeDef::products
	IL_0028: ldc.i4.0
	IL_0029: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class Verse.ThingDefCountClass>::get_Item(int32)
	IL_002e: ldfld class Verse.ThingDef Verse.ThingDefCountClass::thingDef
	IL_0033: ret
*/

//Commented for integrated into Core_SK
/*[HarmonyPatch]
public class AlwaysReturnTheFirstProduct
{
	[HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.ProducedThingDef), MethodType.Getter)]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> inst = instructions.ToList();
		int index = inst.FindIndex(x => x.Is(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<ThingDefCountClass>), nameof(List<ThingDefCountClass>.Count))));
		inst[index + 2].opcode = OpCodes.Br_S;
		inst.RemoveRange(index - 2, 4);
		return inst;
	}
}*/

//Could patch, but will result in "All assets must be load in the main thread warning", so currently only the above patch seems fine without any knowing problem
/*public class MintMenuPatch
{
	[HarmonyTargetMethod]
	public static MethodInfo TargetMethod()
	{
		return AccessTools.Method("DubsMintMenus.Patch_BillStack_DoListing:DoClones");
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		Log.Message("Patching Dubs Mint Menu");
		List<CodeInstruction> inst = instructions.ToList();
		int index = inst.FindIndex(x => x.Is(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(RecipeDef), nameof(RecipeDef.ProducedThingDef))));
		Label UIIconThing = generator.DefineLabel();
		inst[index + 2].labels.Add(UIIconThing);
		index -= 3;
		inst.InsertRange(index, new CodeInstruction[]
		{
			new CodeInstruction(OpCodes.Ldloc_S, 10),
			new CodeInstruction(OpCodes.Ldfld, AccessTools.Field("DubsMintMenus.Patch_BillStack_DoListing+<>c__DisplayClass24_1:fBill")),
			new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Bill), nameof(Bill.recipe))),
			new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(RecipeDef), nameof(RecipeDef.UIIconThing))),
			new CodeInstruction(OpCodes.Brtrue_S, UIIconThing)
		});
		File.WriteAllText("E:\\before.txt", string.Join("\n", instructions.Select(x => x.ToString())));
		File.WriteAllText("E:\\after.txt",string.Join("\n",inst.Select(x => x.ToString())));
		return inst;
	}
}*/