using System.Collections;
using System.Reflection;

namespace Core_SK_Patch;

/// <summary>
/// Ratkin's wandering caravan can retain unresolved pawn references after loading
/// a save. CleanupDeadPawns removes the null from settlerPool and then passes that
/// null to Dictionary.Remove, which throws before the incident can continue.
/// </summary>
[HarmonyPatch]
public static class RatkinWanderingCaravanNullPawnFix
{
    private const string RatkinPackageId = "Solaris.RatkinRaceMod";
    private const string ComponentTypeName = "NewRatkin.GameComponent_WanderingCaravan";

    private static FieldInfo settlerPoolField;

    private static MethodBase TargetMethod()
    {
        Type componentType = AccessTools.TypeByName(ComponentTypeName);
        return componentType == null
            ? null
            : AccessTools.Method(componentType, "CleanupDeadPawns");
    }

    private static bool Prepare()
    {
        if (!ModsConfig.IsActive(RatkinPackageId))
        {
            return false;
        }

        Type componentType = AccessTools.TypeByName(ComponentTypeName);
        settlerPoolField = componentType == null
            ? null
            : AccessTools.Field(componentType, "settlerPool");

        return componentType != null &&
               settlerPoolField != null &&
               TargetMethod() != null;
    }

    [HarmonyPrefix]
    private static void RemoveUnresolvedSettlers(object __instance)
    {
        if (settlerPoolField?.GetValue(__instance) is not IList settlerPool)
        {
            return;
        }

        int removed = 0;
        for (int index = settlerPool.Count - 1; index >= 0; index--)
        {
            if (settlerPool[index] != null)
            {
                continue;
            }

            settlerPool.RemoveAt(index);
            removed++;
        }

        if (removed > 0)
        {
            Log.Warning(
                $"[Core SK Patch] Removed {removed} unresolved pawn reference(s) " +
                "from Ratkin's wandering caravan roster.");
        }
    }
}
