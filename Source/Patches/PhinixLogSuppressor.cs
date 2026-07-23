using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;

/// <summary>
/// Phinix has no logging toggle and forwards its network logger directly to
/// Verse.Log. Remove only those calls while leaving Phinix behavior intact.
/// </summary>
[HarmonyPatch]
public static class PhinixLogSuppressor
{
    private const string PhinixPackageId = "Thomotron.Phinix";
    private const string PhinixClientTypeName = "PhinixClient.Client";

    private static readonly MethodInfo LogMessage =
        AccessTools.Method(typeof(Log), nameof(Log.Message), new[] { typeof(string) });

    private static readonly MethodInfo LogWarning =
        AccessTools.Method(typeof(Log), nameof(Log.Warning), new[] { typeof(string) });

    private static readonly MethodInfo LogError =
        AccessTools.Method(typeof(Log), nameof(Log.Error), new[] { typeof(string) });

    private static Type PhinixClientType => AccessTools.TypeByName(PhinixClientTypeName);

    private static bool Prepare()
    {
        return ModsConfig.IsActive(PhinixPackageId) &&
               PhinixClientType != null &&
               LogMessage != null &&
               LogWarning != null &&
               LogError != null;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type type in AccessTools.GetTypesFromAssembly(PhinixClientType.Assembly))
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
            {
                if (ContainsPhinixLogCall(method))
                {
                    yield return method;
                }
            }
        }
    }

    private static bool ContainsPhinixLogCall(MethodBase method)
    {
        try
        {
            return PatchProcessor.GetOriginalInstructions(method, null).Any(IsPhinixLogCall);
        }
        catch
        {
            return false;
        }
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RemovePhinixLogCalls(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (IsPhinixLogCall(instruction))
            {
                // All targeted overloads consume exactly one string. Popping it
                // keeps the surrounding control flow and stack balanced.
                instruction.opcode = OpCodes.Pop;
                instruction.operand = null;
            }

            yield return instruction;
        }
    }

    private static bool IsPhinixLogCall(CodeInstruction instruction)
    {
        return instruction.Calls(LogMessage) ||
               instruction.Calls(LogWarning) ||
               instruction.Calls(LogError);
    }
}
