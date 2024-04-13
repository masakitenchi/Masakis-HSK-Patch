using System.Reflection.Emit;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class MechChargerPatch
{
    public static float ChargePerTick => ChargePerDay / GenDate.TicksPerDay;

    public static float ChargePerDay { get; set; } = Building_MechCharger.ChargePerDay;

    [HarmonyPatch(typeof(Building_MechCharger),nameof(Building_MechCharger.Tick))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RechargeSpeedPatch(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> instruction = instructions.ToList();
        int index = instruction.FindIndex(x => x.Is(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Need), nameof(Need.CurLevel)))) + 1;
        instruction.Replace(instruction[index], new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(MechChargerPatch), nameof(MechChargerPatch.ChargePerTick))));
        return instruction;
    }
}

public class MechChargerUpgrade : ResearchMod
{
    public override void Apply()
    {
        MechChargerPatch.ChargePerDay *= 2;
    }
}

