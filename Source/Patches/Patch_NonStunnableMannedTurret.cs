using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;


/// <summary>
/// Patches all subclass of Building_Turret. Its PreApplyDamage controls the stunhandler. Checks if they has CompPowerTrader comp.
/// </summary>
[HarmonyPatch]
public static class Patch_NonStunnableMannedTurret
{
    private static readonly MethodInfo PreApplyDamage = AccessTools.Method(typeof(Building_Turret), nameof(Building_Turret.PreApplyDamage));
    private static readonly FieldInfo stunner = AccessTools.Field(typeof(Building_Turret), nameof(Building_Turret.stunner));
    private static readonly MethodInfo check = AccessTools.Method(typeof(Patch_NonStunnableMannedTurret), nameof(HasPowerTrader));


    [HarmonyTargetMethod]
    public static MethodInfo Method()
    {
        return PreApplyDamage;
    }

    /*
             * // stunner.Notify_DamageApplied(dinfo);
	        IL_000d: ldarg.0
                call bool Core_SK_Patch.Patch_NonStunnableMannedTurret::HasPowerTrader()
                brfalse.s ldarg2
                ldarg.0
	        IL_000e: ldfld class RimWorld.StunHandler RimWorld.Building_Turret::stunner
	        IL_0013: ldarg.1
	        IL_0014: ldobj Verse.DamageInfo
	        IL_0019: callvirt instance void RimWorld.StunHandler::Notify_DamageApplied(valuetype Verse.DamageInfo)
	        // absorbed = false;
	        IL_001e: ldarg.2   <- label(ldarg2) here
	        IL_001f: ldc.i4.0
	        IL_0020: stind.i1
	        // (no C# code)
	        IL_0021: ret
     */
    [HarmonyPatch]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SkipStunIfDontUsePower(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        //Log.Message("Patching Building_Turret.PreapplyDamage");
        bool Found = false;
        List<CodeInstruction> instructions2 = instructions.ToList();
        int ldarg2_last = instructions2.FindLastIndex(x => x.opcode == OpCodes.Ldarg_2);
        Label ldarg2 = generator.DefineLabel();
        instructions2[ldarg2_last].labels.Add(ldarg2);
        for (var i = 0; i < instructions2.Count; i++)
        {
            if (!Found && i < ldarg2_last && instructions2[i + 1].Is(OpCodes.Ldfld, stunner) && instructions2[i].IsLdarg(0))
            {
                Found = true;
                instructions2.InsertRange(i + 1, new CodeInstruction[]
                {
                    new CodeInstruction(OpCodes.Call, check),
                    new CodeInstruction(OpCodes.Brfalse_S, ldarg2),
                    new CodeInstruction(OpCodes.Ldarg_0)
                });
                break;
            }
        }
        if (!Found)
            Log.Error(nameof(Patch_NonStunnableMannedTurret) + " Cannot patch. Target IL code not found.");
        //System.IO.File.WriteAllLines("E:\\After.txt", instructions2.Select(x => x.ToString()));
        return instructions2;
    }

    public static bool HasPowerTrader(this Building_Turret building)
    {
        //Log.Message($"Checking {building}'s CompPowerTrader");
        return building.TryGetComp<CompPowerTrader>() is not null;
    }
}
