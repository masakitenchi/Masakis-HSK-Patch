using CombatExtended;
using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

//[DelayedPatch]
[StaticConstructorOnStartup]
public static class Patch_NightVision
{
    private static readonly MethodInfo pawn_GetStatValue = AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue));
    private static readonly FieldInfo NightVision = AccessTools.Field(typeof(CE_StatDefOf), nameof(CE_StatDefOf.NightVisionEfficiency));
    private static readonly MethodInfo TryApply = AccessTools.Method(typeof(HediffGiver), nameof(HediffGiver.TryApply));


    static Patch_NightVision()
    {
        Main.harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Dark), nameof(ThoughtWorker_Dark.CurrentStateInternal)), transpiler: new HarmonyMethod(typeof(Patch_NightVision), nameof(Patch_NightVision.TSP1)));
        Main.harmony.Patch(AccessTools.Method(typeof(SK.Enlighten.HediffGiver_Enlighten), nameof(SK.Enlighten.HediffGiver_Enlighten.OnIntervalPassed)), transpiler: new HarmonyMethod(typeof(Patch_NightVision), nameof(Patch_NightVision.TSP2)));
    }


    public static IEnumerable<CodeInstruction> TSP1(IEnumerable<CodeInstruction> instructions)
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
        return inst;
    }

    //Since both "dark environment" and "bright environment" use the same hediffGiver, it also makes pawn with >50% night vision efficiency cannot receive the "bright environment" hediff.
    //a fair trade, I must say
    public static IEnumerable<CodeInstruction> TSP2(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> inst = instructions.ToList();
        int Call = inst.FirstIndexOf(x => x.Calls(TryApply));
        int branch = -1;
        for (int i = Call; i >= 0; i--)
        {
            if (inst[i].opcode == OpCodes.Ldloc_1)
            {
                branch = i;
                break;
            }
        }
        if (branch == -1)
        {
            Log.Error($"Patch Error: cannot patch SK.Enlighten.HediffGiver_Enlighten");
            return instructions;
        }
        Label br = (Label)inst[branch + 1].operand;
        inst.InsertRange(branch, new CodeInstruction[]
        {
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldsfld, NightVision),
            new (OpCodes.Ldc_I4_1),
            new (OpCodes.Ldc_I4_M1),
            new (OpCodes.Call, pawn_GetStatValue),
            new (OpCodes.Ldc_R4, 0.5f),
            new (OpCodes.Bge_S, br)
        });
        return inst;
    }
}

