using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

// Optional DBH integration: removing a pipe must not lazily rebuild its network.
[HarmonyPatch]
internal static class HygienePipeDeregisterPatch
{
    private static bool Prepare() => AccessTools.TypeByName("DubsBadHygiene.HygienePipeMapComp") != null;

    private static MethodBase TargetMethod() =>
        AccessTools.Method("DubsBadHygiene.HygienePipeMapComp:DeregisterPipe");

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        Type pipeType = AccessTools.TypeByName("DubsBadHygiene.CompPipe");
        MethodInfo getter = AccessTools.PropertyGetter(pipeType, "pipeNet");
        FieldInfo cachedNet = AccessTools.Field(pipeType, "pipeNetRef");
        if (getter == null || cachedNet == null || cachedNet.FieldType != getter.ReturnType)
            throw new InvalidOperationException("SOS2/DBH pipe cleanup patch: incompatible pipe network members.");

        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(getter))
            {
                // Preserve labels/exception blocks and the original null checks.
                // The rest of DeregisterPipe still removes cachedPipes, clears cells,
                // initializes an existing net and marks the grid dirty.
                instruction.opcode = OpCodes.Ldfld;
                instruction.operand = cachedNet;
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 3)
            throw new InvalidOperationException($"SOS2/DBH pipe cleanup patch expected 3 network reads, found {replaced}.");
    }
}
