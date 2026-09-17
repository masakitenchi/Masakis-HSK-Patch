using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

// CompPipe is also attached to fuel consumers, including Rimatomics' plutonium processor.
[HarmonyPatch]
internal static class RimefellerPipeSpawnCleanupPatch
{
    private static bool Prepare() => AccessTools.TypeByName("Rimefeller.CompPipe") != null;

    private static MethodBase TargetMethod() => AccessTools.Method("Rimefeller.CompPipe:PostSpawnSetup");

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var destroy = AccessTools.Method(typeof(Thing), nameof(Thing.Destroy), new[] { typeof(DestroyMode) });
        var calls = code.Where(instruction => instruction.Calls(destroy)).ToList();
        if (calls.Count != 1)
            throw new InvalidOperationException($"Rimefeller pipe cleanup expected one Destroy call, found {calls.Count}.");

        // Replace only overlap cleanup; keep component setup and network registration intact.
        calls[0].opcode = OpCodes.Call;
        calls[0].operand = AccessTools.Method(typeof(RimefellerPipeSpawnCleanupPatch), nameof(DestroyDuplicatePipe));
        return code;
    }

    private static void DestroyDuplicatePipe(Thing existing, DestroyMode mode)
    {
        // A pipe cell can be covered by a multi-cell machine with its own CompPipe.
        // Only actual oil pipelines are disposable duplicates, never connected equipment.
        if (existing.def.defName == "OilPipeline")
            existing.Destroy(mode);
    }
}
