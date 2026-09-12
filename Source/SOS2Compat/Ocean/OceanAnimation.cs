namespace Core_SK_Patch;

// Pure display math. No Rand calls, population writes or wall-clock time: opening
// the map cannot affect gameplay RNG, and pausing/loading preserves the pose.
internal static class OceanAnimation
{
    internal const int Frames = 32;
    internal static double Time(int ticks, int id) => ticks / 60.0 + (id % 997) * 0.137;
    internal static int Frame(double time, double speed) =>
        (int)(time * speed % Frames + Frames) % Frames;
    internal static int FishCount(float population, float capacity) => population <= 0 || capacity <= 0
        ? 0 : System.Math.Min(6, System.Math.Max(1, (int)System.Math.Ceiling(population / capacity * 6)));

    internal static void Swim(double time, int fish, out float x, out float z, out float angle)
    {
        double p = time * (0.22 + fish * 0.017) + fish * 2.1;
        double shift = fish * 0.8;
        x = (float)(0.32 * System.Math.Cos(p) + 0.055 * System.Math.Sin(2 * p + shift));
        z = (float)(0.23 * System.Math.Sin(p) + 0.04 * System.Math.Sin(3 * p + shift));
        double dx = -0.32 * System.Math.Sin(p) + 0.11 * System.Math.Cos(2 * p + shift);
        double dz = 0.23 * System.Math.Cos(p) + 0.12 * System.Math.Cos(3 * p + shift);
        // Unity yaw rotates local +X toward -Z. The fish sprite faces +X.
        angle = (float)(-System.Math.Atan2(dz, dx) * 180 / System.Math.PI);
    }

    internal static void WaterUV(float u, float v, int frame, out float warpedU, out float warpedV)
    {
        double x = u - 0.5, z = v - 0.5;
        double envelope = System.Math.Max(0, 1 - 4 * (x * x + z * z));
        envelope *= envelope; // Keep the circular rim and transparent corners fixed.
        double phase = frame * 2 * System.Math.PI / Frames;
        warpedU = u + (float)(0.018 * envelope * System.Math.Sin(v * 18 + phase));
        warpedV = v + (float)(0.018 * envelope * System.Math.Sin(u * 16 - phase));
    }

    internal static float TailBend(float u, int frame)
    {
        double tail = System.Math.Max(0, 0.7 - u) / 0.7;
        return (float)(0.10 * tail * tail * System.Math.Sin(frame * 2 * System.Math.PI / Frames + u * 5));
    }
}
