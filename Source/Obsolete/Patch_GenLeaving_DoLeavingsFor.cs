using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using static HarmonyLib.Code;

namespace Core_SK_Patch;

/*[HarmonyPatch]
public static class Patch_GenLeaving_DoLeavingsFor
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.Inner(typeof(GenLeaving), "<>c"), "<DoLeavingsFor>b__6_0");
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SteelToPlasteelTranpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldsfld && instruction.operand as FieldInfo == AccessTools.DeclaredField(typeof(ThingDefOf), nameof(ThingDefOf.Steel)))
            {
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.DeclaredField(typeof(ThingDefOf), nameof(ThingDefOf.Plasteel)));
            }
            else
                yield return instruction;
        }
    }
}*/


/*
 * // int count = thingDefCountClass.thingDef.slagDef.smeltProducts.First((ThingDefCountClass pro) => pro.thingDef == ThingDefOf.Steel).count;
		IL_0264: ldloc.s 14
		IL_0266: ldfld class Verse.ThingDef Verse.ThingDefCountClass::thingDef
		IL_026b: ldfld class Verse.ThingDef Verse.ThingDef::slagDef
		IL_0270: ldfld class [mscorlib]System.Collections.Generic.List`1<class Verse.ThingDefCountClass> Verse.ThingDef::smeltProducts
		IL_0275: ldsfld class [mscorlib]System.Func`2<class Verse.ThingDefCountClass, bool> RimWorld.GenLeaving/'<>c'::'<>9__6_0'
		IL_027a: dup
		IL_027b: brtrue.s IL_0294
		IL_027d: pop
		IL_027e: ldsfld class RimWorld.GenLeaving/'<>c' RimWorld.GenLeaving/'<>c'::'<>9'
		IL_0283: ldftn instance bool RimWorld.GenLeaving/'<>c'::'<DoLeavingsFor>b__6_0'(class Verse.ThingDefCountClass)
		IL_0289: newobj instance void class [mscorlib]System.Func`2<class Verse.ThingDefCountClass, bool>::.ctor(object, native int)
		IL_028e: dup
		IL_028f: stsfld class [mscorlib]System.Func`2<class Verse.ThingDefCountClass, bool> RimWorld.GenLeaving/'<>c'::'<>9__6_0'
		IL_0294: call !!0 [System.Core]System.Linq.Enumerable::First<class Verse.ThingDefCountClass>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>, class [mscorlib]System.Func`2<!!0, bool>)
		IL_0299: ldfld int32 Verse.ThingDefCountClass::count

*/
//System.Collections.Generic.List`1.get_Item

//Already integrated into Core_SK.dll
/*[HarmonyPatch]
public static class Patch_GenLeaving_DoLeavingsFor_FirstItemInSmeltProduct
{
    public static readonly FieldInfo FsmeltProduct = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.smeltProducts));
    public static readonly FieldInfo Fcount = AccessTools.Field(typeof(ThingDefCountClass), nameof(ThingDefCountClass.count));

    [HarmonyPatch(typeof(GenLeaving), nameof(GenLeaving.DoLeavingsFor), new System.Type[] { typeof(Thing), typeof(Map), typeof(DestroyMode), typeof(CellRect), typeof(System.Predicate<IntVec3>), typeof(List<Thing>) })]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FirstItemInSmeltProduct(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> code = instructions.ToList();
        int smeltProducts = code.FindIndex(x => x.opcode == OpCodes.Ldfld && x.operand as FieldInfo == FsmeltProduct);
        int count = code.FindLastIndex(x => x.opcode == OpCodes.Ldfld && x.operand as FieldInfo == Fcount);
        if(count <= smeltProducts)
        {
            Log.Error("Transpiler Failed. count shouldn't be less than smeltProducts");
            return instructions;
        }
        if (smeltProducts == -1 || count == -1)
        {
            Log.Error($"Transpiler Failed. Couldn't find {(smeltProducts == -1 ? "SmeltProduct" : "")} {(count == -1 ? "count" : "")}");
            return instructions;
        }
        //Log.Message($"[Core_SK Patch] smeltProduct : {smeltProducts}, count : {count}");
        code.RemoveRange(smeltProducts + 1, count - smeltProducts - 1);
        code.InsertRange(smeltProducts + 1, new List<CodeInstruction>()
        {
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<ThingDefCountClass>), "Item"))
        });
        //System.IO.File.WriteAllLines("E:\\after.txt", code.Select(x => x.ToString()));
        return code;
    }
}*/
