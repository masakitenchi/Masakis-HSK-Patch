using CombatExtended;
using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

//[DelayedPatch]
[StaticConstructorOnStartup]
public static class Patch_NightVision
{
    private static readonly MethodInfo pawn_GetStatValue = AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue));
    //stub to prevent game complaining about failed to find StatDef
    private static readonly FieldInfo NightVision = AccessTools.Field(typeof(CE_StatDefOf), nameof(CE_StatDefOf.NightVisionEfficiency));


    //[HarmonyPatch(typeof(ThoughtWorker_Dark), nameof(ThoughtWorker_Dark.CurrentStateInternal))]
    /*public static bool Nothing(ref ThoughtState __result, Pawn p)
    {
        __result = p.GetStatValue(CombatExtended.CE_StatDefOf.NightVisionEfficiency) >= 0.5 ? false : __result;
        return false;
    }*/
    static Patch_NightVision() 
    {
        Main.harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Dark), nameof(ThoughtWorker_Dark.CurrentStateInternal)), transpiler: new HarmonyMethod(typeof(Patch_NightVision),"Transpiler"));
    }


    //[HarmonyPatch(typeof(ThoughtWorker_Dark), nameof(ThoughtWorker_Dark.CurrentStateInternal))]
    //[HarmonyDebug]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> inst = instructions.ToList();
        Label? label = null;
        int brfalse = inst.FirstIndexOf(x => x.Branches(out label));
        inst.InsertRange(0, new CodeInstruction[]
        {
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldsfld, NightVision),
            new (OpCodes.Ldc_I4_1),
            new (OpCodes.Ldc_I4_M1),
            new (OpCodes.Call, pawn_GetStatValue),
            new (OpCodes.Ldc_R4, 0.5f),
            new (OpCodes.Bge_S, label)
        });
        //System.IO.File.WriteAllLines("E:\\after.txt", inst.Select(x => x.ToString()));
        return inst;
    }
}

