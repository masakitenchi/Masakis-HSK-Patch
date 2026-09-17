using System.Reflection;
using System.Reflection.Emit;
using SaveOurShip2;

namespace Core_SK_Patch;

// Keep SOS2's asteroid shapes and encounters, but use HSK's standard resource deposits.
[HarmonyPatch]
internal static class ValuableAsteroidMineralsPatch
{
    private static readonly string[] MineralDefNames =
    {
        "MineableSteel", "MineableSilver", "MineableGold", "MineableUranium",
        "MineablePlasteel", "MineableJade", "MineableComponentsIndustrial",
        "MineableNitre", "MineableCoal", "MineableTin", "MineableCopper",
        "MineableAluminium", "MineableTitanium", "MineableTungsten", "MineableNickel",
        "MineableLead", "MineableGlowstone", "MineableSalt", "MineableColdstone",
        // Optional ores from Core_SK_Patch's Material Science integration.
        "MineableSphalerite", "MineableQuartz"
    };

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(GenStep_ValuableAsteroids), "GenerateBigAsteroid");
        yield return AccessTools.Method(typeof(GenStep_ValuableAsteroids), "GenerateSmallAsteroid");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var selections = code.Where(instruction => instruction.opcode == OpCodes.Call
            && instruction.operand is MethodInfo method
            && method.DeclaringType == typeof(GenCollection)
            && method.Name == nameof(GenCollection.RandomElement)
            && method.IsGenericMethod
            && method.GetGenericArguments().SequenceEqual(new[] { typeof(ThingDef) })).ToList();
        if (selections.Count != 2)
            throw new InvalidOperationException($"SOS2 asteroid patch expected rock and mineral selections, found {selections.Count}.");

        // The first selection chooses the stone shell; only replace the second (ore) selection.
        selections[1].operand = AccessTools.Method(typeof(ValuableAsteroidMineralsPatch), nameof(SelectMineral));
        return code;
    }

    private static ThingDef SelectMineral(IEnumerable<ThingDef> original)
    {
        // Do not enumerate the original pool: Minerals crystals use custom rare drops.
        var pool = MineralDefNames.Select(name => DefDatabase<ThingDef>.GetNamedSilentFail(name))
            .Where(IsValidDeposit).ToList();
        if (pool.Count == 0)
            throw new InvalidOperationException("SOS2Compat: No valid HSK deposits for valuable asteroids.");
        return pool.RandomElement();
    }

    private static bool IsValidDeposit(ThingDef def)
    {
        var building = def?.building;
        if (building == null || !building.isNaturalRock || !building.isResourceRock
            || building.mineableThing == null || building.mineableYield <= 0)
            return false;
        float value = building.mineableYield * building.mineableThing.BaseMarketValue;
        // SOS2 truncates this value to int before division; exclude zero, NaN and overflow.
        return value >= 1f && (double)value <= int.MaxValue;
    }
}

[HarmonyPatch(typeof(GenStep_ValuableAsteroids), nameof(GenStep_ValuableAsteroids.Generate))]
internal static class ValuableAsteroidRegionCleanupPatch
{
    private static void Prefix(Map map, out bool __state) => __state = map.regionAndRoomUpdater.Enabled;

    // Restore the previous state even if generation throws; preserve the original exception.
    private static void Finalizer(Map map, bool __state) => map.regionAndRoomUpdater.Enabled = __state;
}
