
using HarmonyLib;
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
[HarmonyPatch]
public class AlwaysReturnTheFirstProduct
{
    [HarmonyPrepare]
    public static bool Prepare()
    {
        return false;
    }

    [HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.ProducedThingDef), MethodType.Getter)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> inst = instructions.ToList();
        int index = inst.FindIndex(x => x.opcode == OpCodes.Beq_S);
        inst[index].opcode = OpCodes.Bge_S;
        return inst;
    }
}

//Could patch, but will result in "All assets must be load in the main thread warning", so currently only the above patch seems fine without any knowing problem
//Need this Attribute to patch the patch before applying patch
[StaticConstructorOnStartup]
public static class MintMenuPatch
{
    static MethodBase Doclones = AccessTools.Method("DubsMintMenus.Patch_BillStack_DoListing:DoClones");
    static MethodBase DoRow = AccessTools.Method("DubsMintMenus.Patch_BillStack_Dolisting:DoRow");

    static MintMenuPatch()
    {
        Core_SK_Patch.harmony.Patch(AccessTools.Method("DubsMintMenus.Patch_BillStack_DoListing:DoClones"), transpiler: new HarmonyMethod(typeof(MintMenuPatch), nameof(MintMenuPatch.Transpiler)));
        Core_SK_Patch.harmony.Patch(AccessTools.Method("DubsMintMenus.Patch_BillStack_DoListing:DoRow"), transpiler: new HarmonyMethod(typeof(MintMenuPatch), nameof(MintMenuPatch.Transpiler)));
    }


    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __original)
    {
        Log.Message("Patching Dubs Mint Menu");
        List<CodeInstruction> inst = instructions.ToList();
        bool found = false;
        int i;
        for (i = 0; i < inst.Count; i++)
        {
            //For all if(recipe.ProducedThingDef != null)
            if (inst[i].Is(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(RecipeDef), nameof(RecipeDef.ProducedThingDef))) && inst[i + 1].Branches(out var _))
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            Log.Error($"Old transpiler in {nameof(MintMenuPatch)}");
            return instructions;
        }
        Label UIIconThing = generator.DefineLabel();
        inst[i + 2].labels.Add(UIIconThing);
        //Need to insert the instructions before recipe.ProducedThingDef != null
        while (!inst[i].IsLdloc() && !inst[i].IsLdarg()) i--;
        //DoRow has RecipeDef as its parameter while DoClones uses an enumerator
        if (__original == Doclones)
        {
            inst.InsertRange(i, new CodeInstruction[]
            {
                //Ldloc_S -> the enumerator
                new CodeInstruction(OpCodes.Ldloc_S, inst[i].operand),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field("DubsMintMenus.Patch_BillStack_DoListing+<>c__DisplayClass24_1:fBill")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Bill), nameof(Bill.recipe))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(RecipeDef), nameof(RecipeDef.UIIconThing))),
                new CodeInstruction(OpCodes.Brtrue_S, UIIconThing)
            });
        }
        else
        {
            inst.InsertRange(i, new CodeInstruction[]
            {
				//Ldarg.0 -> RecipeDef
                new CodeInstruction(inst[i].opcode),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(RecipeDef), nameof(RecipeDef.UIIconThing))),
                new CodeInstruction(OpCodes.Brtrue_S, UIIconThing)
            });
        }
        //File.WriteAllText($"E:\\before{__original.Name}.txt", string.Join("\n", instructions.Select(x => x.ToString())));
        //File.WriteAllText($"E:\\after{__original.Name}.txt", string.Join("\n", inst.Select(x => x.ToString())));
        return inst;
    }

}