using System.Runtime.CompilerServices;

namespace Core_SK_Patch;

public static class Utilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Invert_Bool(ref this bool a)
    {
        a = !a;
    }
}
