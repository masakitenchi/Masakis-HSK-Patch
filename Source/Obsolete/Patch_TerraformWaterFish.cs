using System.Reflection;

namespace Core_SK_Patch;

[HarmonyPatch]
internal static class Patch_TerraformWaterFish
{
    private struct TerraformState
    {
        public Map Map;
        public IntVec3 Cell;
        public TerrainDef OldTerrain;
    }

    private static Type TerraformType => AccessTools.TypeByName("FertileFields.Building_Terraform");

    private static bool Prepare()
    {
        return ModsConfig.OdysseyActive && TerraformType != null;
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(TerraformType, "PlaceProduct");
    }

    [HarmonyPrefix]
    private static void Prefix(Thing __instance, out TerraformState __state)
    {
        __state = default;
        Map map = __instance?.Map;
        if (map == null)
        {
            return;
        }

        IntVec3 cell = __instance.Position;
        if (!cell.InBounds(map))
        {
            return;
        }

        __state = new TerraformState
        {
            Map = map,
            Cell = cell,
            OldTerrain = map.terrainGrid.TerrainAt(cell)
        };
    }

    [HarmonyPostfix]
    private static void Postfix(TerraformState __state)
    {
        Map map = __state.Map;
        IntVec3 cell = __state.Cell;
        if (map == null || !cell.InBounds(map))
        {
            return;
        }

        TerrainDef newTerrain = map.terrainGrid.TerrainAt(cell);
        WaterBodyType oldType = __state.OldTerrain?.waterBodyType ?? WaterBodyType.None;
        WaterBodyType newType = newTerrain?.waterBodyType ?? WaterBodyType.None;
        if (oldType != WaterBodyType.None ||
            (newType != WaterBodyType.Freshwater && newType != WaterBodyType.Saltwater))
        {
            return;
        }

        WaterBody body = FishingUtility.GetWaterBody(cell, map);
        if (body == null)
        {
            // SetTerrain normally notifies the tracker. Keep the same fallback used by
            // the HSK/Odyssey fishing bridge for modded aquatic terrain edge cases.
            map.waterBodyTracker.Notify_TerrainChanged(cell, null, newTerrain);
            body = FishingUtility.GetWaterBody(cell, map);
        }

        if (body == null || body.CommonFish.Any() || body.UncommonFish.Any())
        {
            return;
        }

        // Reuse Odyssey's own size-based chance and biome fish selection. Artificial
        // water bodies start at one cell, so their initial roll commonly has no fish;
        // each completed expansion now gets another normal roll at its current size.
        body.SetFishTypes();
    }
}
