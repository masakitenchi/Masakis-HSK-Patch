using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Core_SK_Patch;

[StaticConstructorOnStartup]
public static class RimThemesSoundHotSwitchFixBootstrap
{
    static RimThemesSoundHotSwitchFixBootstrap()
    {
        if (!ModsConfig.IsActive("arandomkiwi.rimthemes_steam"))
        {
            return;
        }
        new Harmony("com.reggex.HSKPatch.RimThemesSoundHotSwitchFix")
            .CreateClassProcessor(typeof(RimThemesSoundHotSwitchFix))
            .Patch();
    }
}

/// <summary>
/// RimThemes can cache SoundDefOf's resolved grains before FasterGameLoading has
/// resolved them.  A later hot theme switch then "restores" those empty lists,
/// leaving the UI sounds permanently silent until the game is restarted.
/// </summary>
[HarmonyPatch]
public static class RimThemesSoundHotSwitchFix
{
    private static readonly FieldInfo ResolvedGrainsField =
        AccessTools.Field(typeof(SubSoundDef), "resolvedGrains");

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("aRandomKiwi.RimThemes.Themes:changeSoundTheme");
    }

    private static bool Prepare()
    {
        return TargetMethod() != null && ResolvedGrainsField != null;
    }

    [HarmonyPrefix]
    private static void RefreshPrematureVanillaCache(MethodBase __originalMethod)
    {
        try
        {
            Type themesType = __originalMethod.DeclaringType;
            FieldInfo cacheField = AccessTools.Field(themesType, "DBVanillaResolvedGrains");
            FieldInfo savedField = AccessTools.Field(themesType, "vanillaGrainsSaved");

            if (cacheField?.GetValue(null) is not Dictionary<string, List<ResolvedGrain>> cache ||
                savedField?.GetValue(null) is not true ||
                !CacheMissesResolvedSound(cache))
            {
                return;
            }

            // The first cache was made too early.  By the time the player can
            // switch a theme, FasterGameLoading has populated the live lists.
            if (!SoundDefOfSubSounds().Any(subSound => ResolvedGrains(subSound)?.Count > 0))
            {
                return;
            }

            cache.Clear();
            AccessTools.Method(themesType, "saveVanillaGrains")?.Invoke(null, null);

            int recovered = cache.Values.Count(grains => grains is { Count: > 0 });
            if (recovered > 0)
            {
                Log.Message($"[Core SK Patch] Refreshed RimThemes' premature UI sound cache ({recovered} sounds).");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"[Core SK Patch] Could not refresh RimThemes' UI sound cache: {exception.Message}");
        }
    }

    [HarmonyPostfix]
    private static void ResolveAnyEmptyFallbacks()
    {
        int repaired = 0;

        foreach (SubSoundDef subSound in SoundDefOfSubSounds())
        {
            List<ResolvedGrain> resolved = ResolvedGrains(subSound);
            if (resolved is not { Count: 0 } || subSound.grains.NullOrEmpty())
            {
                continue;
            }

            try
            {
                subSound.ResolveReferences();
                if (ResolvedGrains(subSound)?.Count > 0)
                {
                    repaired++;
                }
            }
            catch (Exception exception)
            {
                Log.Warning($"[Core SK Patch] Could not re-resolve a RimThemes UI sound: {exception.Message}");
            }
        }

        if (repaired > 0)
        {
            Log.Message($"[Core SK Patch] Re-resolved {repaired} empty UI sounds after a RimThemes switch.");
        }
    }

    private static IEnumerable<SubSoundDef> SoundDefOfSubSounds()
    {
        foreach (FieldInfo field in typeof(SoundDefOf).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not SoundDef soundDef || soundDef.subSounds.NullOrEmpty())
            {
                continue;
            }

            foreach (SubSoundDef subSound in soundDef.subSounds)
            {
                if (subSound != null)
                {
                    yield return subSound;
                }
            }
        }
    }

    private static bool CacheMissesResolvedSound(Dictionary<string, List<ResolvedGrain>> cache)
    {
        foreach (FieldInfo field in typeof(SoundDefOf).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not SoundDef soundDef || soundDef.subSounds.NullOrEmpty())
            {
                continue;
            }

            List<ResolvedGrain> live = ResolvedGrains(soundDef.subSounds[0]);
            if (live is { Count: > 0 } &&
                (!cache.TryGetValue(field.Name, out List<ResolvedGrain> saved) || saved.NullOrEmpty()))
            {
                return true;
            }
        }

        return false;
    }

    private static List<ResolvedGrain> ResolvedGrains(SubSoundDef subSound)
    {
        return ResolvedGrainsField.GetValue(subSound) as List<ResolvedGrain>;
    }
}
