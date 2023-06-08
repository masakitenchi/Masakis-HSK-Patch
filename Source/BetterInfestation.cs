using RimWorld;
using Verse;
using HarmonyLib;
using System.IO;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class BetterInfestation
{
    private static readonly MethodInfo InHorDistOf = AccessTools.Method(typeof(IntVec3), nameof(IntVec3.InHorDistOf));

    [HarmonyPatch(typeof(CompCreatesInfestations), nameof(CompCreatesInfestations.CantFireBecauseSomethingElseCreatedInfestationRecently), MethodType.Getter)]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        if (InHorDistOf == null)
        {
            Core_SK_Patch.sb.AppendLine("Error: IntVec3.InHorDistOf not found.");
            return instructions;
        }
        bool found = false;
        List<CodeInstruction> codes = instructions.ToList();
        for (var i = 0; i < codes.Count - 2; i++)
        {
            // Verse.IntVec3::InHorDistOf(valuetype Verse.IntVec3, float32), so the previous IL Code must be loading a float32 onto the stack
            if (codes[i].operand as MethodInfo == InHorDistOf)
            {
                // Alternative
                // codes[i - 1] = CodeInstruction.LoadField(typeof(Settings), "InfestationPreventionRadius");
                codes[i - 1].opcode = OpCodes.Ldsfld;
                codes[i - 1].operand = AccessTools.Field(typeof(Settings), nameof(Settings.InfestationPreventionRadius));
                found = true;
                break;
            }
        }
        if (!found)
        {
            Core_SK_Patch.sb.AppendLine("Error: Cannot Patch DeepDrill Infestation. Please Contat the author.");
            return instructions;
        }
        /*File.WriteAllLines("E:\\Validatorbefore.txt", instructions.Select(x => x.ToString()));
        File.WriteAllLines("E:\\Validatorafter.txt", codes.Select(x => x.ToString()));*/
        return codes;
    }
}
