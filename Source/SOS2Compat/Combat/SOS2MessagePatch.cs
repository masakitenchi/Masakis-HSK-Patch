using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

// Redirect only SOS2 call sites; no global log filter or per-message stack walk.
[HarmonyPatch]
internal static class SOS2MessagePatch
{
    private static readonly MethodInfo StringLog = AccessTools.Method(typeof(Log), nameof(Log.Message), new[] { typeof(string) });
    private static readonly MethodInfo ObjectLog = AccessTools.Method(typeof(Log), nameof(Log.Message), new[] { typeof(object) });
    private static readonly MethodInfo StringWrapper = AccessTools.Method(typeof(SOS2MessagePatch), nameof(Message), new[] { typeof(string) });
    private static readonly MethodInfo ObjectWrapper = AccessTools.Method(typeof(SOS2MessagePatch), nameof(Message), new[] { typeof(object) });

    private static IEnumerable<MethodBase> TargetMethods()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (Type type in typeof(SaveOurShip2.ShipInteriorMod2).Assembly.GetTypes())
        {
            IEnumerable<MethodBase> methods = type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags));
            if (type.TypeInitializer != null)
                methods = methods.Append(type.TypeInitializer);
            foreach (MethodBase method in methods.Distinct())
            {
                if (method.ContainsGenericParameters || method.GetMethodBody() == null)
                    continue;
                if (PatchProcessor.GetOriginalInstructions(method).Any(IsMessageCall))
                    yield return method;
            }
        }
    }

    // Warnings/errors and logs emitted by other assemblies are deliberately untouched.
    // Iterator and closure types are included in the assembly scan above.
    private static bool IsMessageCall(CodeInstruction instruction) =>
        instruction.Calls(StringLog) || (ObjectLog != null && instruction.Calls(ObjectLog));

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (IsMessageCall(instruction))
            {
                instruction.operand = instruction.Calls(StringLog) ? StringWrapper : ObjectWrapper;
                instruction.opcode = OpCodes.Call;
            }
            yield return instruction;
        }
    }

    internal static void Message(string text)
    {
        if (!Settings.SuppressSOS2Messages)
            Log.Message(text);
    }

    internal static void Message(object value)
    {
        if (!Settings.SuppressSOS2Messages)
            Log.Message(value);
    }
}
