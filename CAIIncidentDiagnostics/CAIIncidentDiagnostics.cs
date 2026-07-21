using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core_SK_Patch.CAIIncidentDiagnostics;

[StaticConstructorOnStartup]
internal static class Bootstrap
{
    static Bootstrap()
    {
        if (!ModsConfig.IsActive(CAIIncidentDiagnostics.CAIPackageId))
        {
            return;
        }

        new Harmony("com.reggex.HSKPatch.CAIIncidentDiagnostics").PatchAll();
    }
}

[HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
internal static class CAIIncidentDiagnostics
{
    internal const string CAIPackageId = "krkr.rule56";
    private const string CAIHarmonyId = "Krkr.Rule56";

    [HarmonyFinalizer]
    [HarmonyPriority(Priority.First)]
    [HarmonyBefore(CAIHarmonyId)]
    private static Exception Finalizer(IncidentWorker __instance, IncidentParms parms, Exception __exception)
    {
        if (__exception == null)
        {
            return null;
        }

        IncidentDef incidentDef = __instance?.def;
        StringBuilder message = new StringBuilder();
        message.AppendLine("[Core_SK_Patch][CAI incident diagnostics] An incident threw before CAI rethrew the exception.");
        message.AppendLine($"IncidentDef: {incidentDef?.defName ?? "<null>"}");
        message.AppendLine($"Worker: {__instance?.GetType().FullName ?? "<null>"}");
        message.AppendLine($"Category: {incidentDef?.category?.defName ?? "<null>"}");
        message.AppendLine($"Target: {Describe(parms?.target)}");
        message.AppendLine($"Faction: {parms?.faction?.def?.defName ?? "<null>"}");
        message.AppendLine($"Points: {(parms == null ? "<null>" : parms.points.ToString("0.##"))}");
        message.AppendLine($"CAI fog of war enabled: {GetCAIFogOfWarState()}");
        message.AppendLine("Original exception:");
        message.Append(__exception);
        Log.Error(message.ToString());

        return __exception;
    }

    private static string Describe(object value)
    {
        if (value == null)
        {
            return "<null>";
        }

        try
        {
            return $"{value.GetType().FullName}: {value}";
        }
        catch
        {
            return value.GetType().FullName;
        }
    }

    private static string GetCAIFogOfWarState()
    {
        try
        {
            Type finderType = AccessTools.TypeByName("CombatAI.Finder");
            FieldInfo settingsField = AccessTools.Field(finderType, "Settings");
            object settings = settingsField?.GetValue(null);
            FieldInfo fogField = settings == null ? null : AccessTools.Field(settings.GetType(), "FogOfWar_Enabled");
            return fogField?.GetValue(settings)?.ToString() ?? "<unknown>";
        }
        catch (Exception exception)
        {
            return $"<unavailable: {exception.GetType().Name}>";
        }
    }
}
