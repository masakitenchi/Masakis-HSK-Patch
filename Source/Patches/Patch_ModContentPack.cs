using System.Reflection;
using System.Reflection.Emit;
using System.IO;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class Patch_ModContentPack
{
    private static readonly MethodInfo Format2 = AccessTools.Method(typeof(string), nameof(string.Format), new Type[] { typeof(string), typeof(object) });
    private static readonly MethodInfo Format3 = AccessTools.Method(typeof(string), nameof(string.Format), new Type[] { typeof(string), typeof(object), typeof(object) });

    private static readonly MethodInfo Error = AccessTools.Method(typeof(Verse.Log), nameof(Verse.Log.Error), new Type[] { typeof(string) });
    private static readonly MethodInfo get_Item = AccessTools.Method(typeof(List<LoadableXmlAsset>), "get_Item");
    private static readonly MethodInfo FullFolderPath = AccessTools.PropertyGetter(typeof(LoadableXmlAsset), nameof(LoadableXmlAsset.FullFilePath));

    /*          IL_00ab: ldstr "Unexpected element in patch XML; got {0}, expected 'Operation'"
				IL_00b0: ldloc.s 4
				IL_00b2: callvirt instance string [System.Xml]System.Xml.XmlNode::get_Name()
				IL_00b7: call string [mscorlib]System.String::Format(string, object)
				IL_00bc: call void Verse.Log::Error(string)
				// continue;
				IL_00c1: br.s IL_00f4
    */

    [HarmonyPrepare]
    public static bool Check()
    {
        bool passed = true;
        foreach(var field in typeof(Patch_ModContentPack).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if(field.GetValue(null) == null)
            {
                Log.Error($"{field.Name} is null, skipping patch");
                passed = false;
            }
        }
        return passed;
    }

    [HarmonyPriority(800)]
    [HarmonyPatch(typeof(ModContentPack), nameof(ModContentPack.LoadPatches))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ShowFileName(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        bool foundstr = false;
        bool foundFormat = false;
        //StreamWriter sb = File.CreateText("E:\\afterRep.txt");
        //File.WriteAllLines("E:\\beforeRep.txt", instructions.Select(x => x.ToString()));
        foreach (var inst in instructions)
        {
            if (!foundstr && inst.Is(OpCodes.Ldstr, "Unexpected element in patch XML; got {0}, expected 'Operation'"))
            {
                inst.operand = "Unexpected element in patch XML; got {0}, expected 'Operation'. \nPath: {1}";
                foundstr = true;
                //sb.WriteLine(inst.ToString());
                yield return inst;
                continue;
            }
            if (foundstr && !foundFormat && inst.Is(OpCodes.Call, Format2))
            {
                foundFormat = true;
                inst.operand = Format3;
                //sb.WriteLine(new CodeInstruction(OpCodes.Ldloc_0).ToString());
                yield return new CodeInstruction(OpCodes.Ldloc_0);
                //sb.WriteLine(new CodeInstruction(OpCodes.Ldloc_1).ToString());
                yield return new CodeInstruction(OpCodes.Ldloc_1);
                //sb.WriteLine(new CodeInstruction(OpCodes.Callvirt, get_Item).ToString());
                yield return new CodeInstruction(OpCodes.Callvirt, get_Item);
                //sb.WriteLine(new CodeInstruction(OpCodes.Callvirt, FullFolderPath).ToString());
                yield return new CodeInstruction(OpCodes.Callvirt, FullFolderPath);
                //sb.WriteLine(inst.ToString());
                yield return inst;
                continue;
            }
            //sb.WriteLine(inst.ToString());
            yield return inst;
        }
        if(!foundstr || !foundFormat)
        {
            Log.Error("Cannot patch");
        }
    }


}
