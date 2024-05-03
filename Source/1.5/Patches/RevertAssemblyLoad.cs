using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class RevertAssemblyLoadMethod
{
    private static readonly MethodInfo get_FullName = AccessTools.PropertyGetter(typeof(FileSystemInfo), nameof(FileSystemInfo.FullName));
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod() => AccessTools.Method(typeof(ModAssemblyHandler), nameof(ModAssemblyHandler.ReloadAll));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RevertAssemblyLoad(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        foreach (var inst in instructions)
        {
            if (inst.Is(OpCodes.Callvirt, get_FullName))
            {
                yield return inst;
                yield return new(OpCodes.Call, AccessTools.Method(typeof(RevertAssemblyLoadMethod), nameof(LoadAssemblyBytes)));
            }
            else if (inst.Is(OpCodes.Call, AccessTools.Method(typeof(Assembly), nameof(Assembly.LoadFile), new Type[] { typeof(string) })))
            {
                yield return new(OpCodes.Call, AccessTools.Method(typeof(Assembly), nameof(Assembly.Load), new Type[] { typeof(byte[]) }));
            }
            else
            {
                yield return inst;
            }
        }
    }

    private static byte[] LoadAssemblyBytes(string path)
    {
        return File.ReadAllBytes(path);
    }
}
