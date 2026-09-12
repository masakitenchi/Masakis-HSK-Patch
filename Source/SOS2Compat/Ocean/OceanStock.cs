namespace Core_SK_Patch;

// Pure arithmetic shared by the building and regression tests. Population is a
// resource, not elapsed wall time: unloaded/minified devices cannot recharge.
internal static class OceanStock
{
    internal static float Normalize(float stock, float capacity) =>
        float.IsNaN(stock) || float.IsInfinity(stock) ? 0f : System.Math.Max(0f, System.Math.Min(capacity, stock));

    internal static float Recharge(float stock, float capacity, float perDay, int ticks, bool powered) =>
        Normalize(Normalize(stock, capacity) + (powered ? System.Math.Max(0f, perDay) * System.Math.Max(0, ticks) / 60000f : 0f), capacity);

    internal static int CatchCount(float stock, float requested, int stackLimit)
    {
        if (float.IsNaN(requested) || float.IsInfinity(requested) || requested <= 0f || stackLimit <= 0)
            return 0;
        return (int)System.Math.Max(0, System.Math.Min(System.Math.Floor(stock),
            System.Math.Min(stackLimit, System.Math.Max(1, System.Math.Round(requested)))));
    }
}
