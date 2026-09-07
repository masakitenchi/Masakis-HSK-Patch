using System.Reflection.Emit;
using Rimatomics;

namespace Core_SK_Patch;

[HarmonyPatch(typeof(Turbine), nameof(Turbine.Tick))]
internal static class CoolingConsumerPatch
{
    private static bool Prepare() => (ModLister.GetActiveModWithIdentifier("dubwise.rimatomics", ignorePostfix: true) != null) &&
        (ModLister.GetActiveModWithIdentifier("kentington.saveourship2", ignorePostfix: true) != null);

    // The turbine computes its current demand immediately before reading cooling.
    // Refresh here, rather than in a Prefix using the previous tick's demand.
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var demandField = AccessTools.Field(typeof(Turbine), nameof(Turbine.UncappedPowerGeneration));
        int matches = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (instruction.opcode == OpCodes.Stfld && Equals(instruction.operand, demandField))
            {
                matches++;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                    typeof(RimatomicsSOS2HeatBridgeRuntime), nameof(RimatomicsSOS2HeatBridgeRuntime.RefreshBeforeConsumer)));
            }
        }
        if (matches != 1)
            throw new InvalidOperationException($"SOS2 cooling bridge expected one turbine demand assignment, found {matches}.");
    }
}
