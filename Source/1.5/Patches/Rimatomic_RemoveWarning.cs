using System.Reflection;
using System.Reflection.Emit;
using System.IO;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class Rimatomic_RemoveWarning
{
    private static readonly MethodInfo get_Name = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Name));
	private static readonly MethodInfo Warning = AccessTools.Method(typeof(Verse.Log), nameof(Log.Warning), new Type[] {typeof(string)});
	private static readonly MethodInfo PatchBase = AccessTools.Method("Rimatomics.DubUtils:applyRads");

	//Added a check since this has been already integrated into base HSK
	public static bool Prepare()
	{
		bool passed = true;
		if (Harmony.GetPatchInfo(PatchBase) != null) return false;
		foreach(var field in typeof(Rimatomic_RemoveWarning).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
		{
			if (field.GetValue(null) == null)
            {
                Logger.Error($"{field.Name} is null. Skipping Transpiler");
				passed = false;
            }
		}
		return ModsConfig.IsActive("dubwise.rimatomics") && passed;
	}
    /*
     * // Log.Warning(pawn.Name?.ToString() + " filteredStrength   " + num);
	IL_00b5: ldarg.0
	IL_00b6: callvirt instance class ['Assembly-CSharp']Verse.Name ['Assembly-CSharp']Verse.Pawn::get_Name()
	IL_00bb: dup
	IL_00bc: brtrue.s IL_00c2

	// (no C# code)
	IL_00be: pop
	IL_00bf: ldnull
	IL_00c0: br.s IL_00c7

	IL_00c2: callvirt instance string [mscorlib]System.Object::ToString()

	IL_00c7: ldstr " filteredStrength   "
	IL_00cc: ldloca.s 0
	IL_00ce: call instance string [mscorlib]System.Single::ToString()
	IL_00d3: call string [mscorlib]System.String::Concat(string, string, string)
	IL_00d8: call void ['Assembly-CSharp']Verse.Log::Warning(string)
	*/

    /*// Log.Warning(pawn.Name?.ToString() + "  " + num2);
	IL_012f: nop
	IL_0130: ldarg.0
	IL_0131: callvirt instance class ['Assembly-CSharp']Verse.Name ['Assembly-CSharp']Verse.Pawn::get_Name()
	IL_0136: dup
	IL_0137: brtrue.s IL_013d

	// (no C# code)
	IL_0139: pop
	IL_013a: ldnull
	IL_013b: br.s IL_0142

	IL_013d: callvirt instance string [mscorlib]System.Object::ToString()

	IL_0142: ldstr "  "
	IL_0147: ldloca.s 1
	IL_0149: call instance string [mscorlib]System.Single::ToString()
	IL_014e: call string [mscorlib]System.String::Concat(string, string, string)
	IL_0153: call void ['Assembly-CSharp']Verse.Log::Warning(string)
	*/
    [HarmonyPatch("Rimatomics.DubUtils", "applyRads")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RemoveWarning(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> inst = instructions.ToList();
		List<int> indexes = new List<int>();
        int i;
		bool flag = false;
        for (i = 0; i < inst.Count; i++)
        {
            if (!flag && inst[i].operand as MethodInfo == get_Name)
            {
				flag = true;
                indexes.Add(i - 1);
				continue;
            }
			if (flag && inst[i].operand as MethodInfo == Warning)
            {
				flag = false;
                indexes.Add(i);
				continue;
            }
        }
		//File.WriteAllLines("E:\\Lines.txt", indexes.Select(x => x.ToString()));
		//File.WriteAllLines("E:\\before.txt", instructions.Select(x => x.ToString()));
        inst.RemoveRange(indexes[2], indexes[3] - indexes[2] + 1);
        inst.RemoveRange(indexes[0], indexes[1] - indexes[0] + 1);
		//File.WriteAllLines("E:\\after.txt", inst.Select(x => x.ToString()));
		return inst;
    }
}
