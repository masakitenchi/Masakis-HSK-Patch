using RimWorld;
using Verse;
using HarmonyLib;
using Steamworks;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class BetterInfestation
{
    [HarmonyPatch(typeof(CompCreatesInfestations), nameof(CompCreatesInfestations.CantFireBecauseSomethingElseCreatedInfestationRecently), MethodType.Getter)]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        bool found = false;
        List<CodeInstruction> codes = instructions.ToList();
        for (var i = 0; i < codes.Count - 2 ; i++)
        {
            if (codes[i].opcode == OpCodes.Ldfld && codes[i + 1].opcode == OpCodes.Callvirt && codes[i + 2].opcode == OpCodes.Ldc_R4)
            {
                codes[i + 2].operand = Settings.InfestationPreventionRadius;
                found = true;
                break;
            }
        }
        if (!found)
        {
            Core_SK_Patch.sb.AppendLine("Error: Cannot Patch DeepDrill Infestation. Please Contat the author.");
            return instructions;
        }
        return codes;
    }
}
