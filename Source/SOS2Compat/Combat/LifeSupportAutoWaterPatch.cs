using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

// Gate DBH's existing generation without replacing tank, pump or pipe logic.
[HarmonyPatch]
internal static class LifeSupportAutoWaterPatch
{
    private static bool Prepare() => AccessTools.TypeByName("DubsBadHygiene.CompWaterStorage") != null;

    private static MethodBase TargetMethod() =>
        AccessTools.Method("DubsBadHygiene.CompWaterStorage:CompTick");

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        FieldInfo rate = AccessTools.Field("DubsBadHygiene.CompProperties_WaterStorage:AutoGenRate");
        MethodInfo adjust = AccessTools.Method(typeof(LifeSupportAutoWaterPatch), nameof(GenerationRate));
        int matches = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (rate != null && instruction.LoadsField(rate))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, adjust);
                matches++;
            }
        }
        if (matches != 1)
            throw new InvalidOperationException($"SOS2/DBH auto water patch expected one generation rate read, found {matches}.");
    }

    // Read the setting on each tick so toggling it needs no network rebuild.
    // Other DBH tanks retain their original generation rate, even on the same pipe net.
    internal static float GenerationRate(float original, ThingComp storage) =>
        !Settings.SOS2LifeSupportAutoWater && storage?.parent?.def?.defName.Contains("Ship_LifeSupport") == true
            ? 0f
            : original;
}
