﻿global using HarmonyLib;
global using System.Collections.Generic;
global using SeedsPlease;
global using RimWorld;
global using Verse;
global using Core_SK_Patch;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class SeedPatch
{
    [HarmonyPatch(typeof(ThingDef),nameof(ThingDef.SpecialDisplayStats))]
    [HarmonyPostfix]
    public static IEnumerable<StatDrawEntry> AddSkillRequirement(IEnumerable<StatDrawEntry> results, ThingDef __instance)
    {
        foreach(var entry in results)
        {
            yield return entry;
        }
        if(__instance is SeedDef seed)
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsImportant, "SkillRequirement",
                seed.plant.plant.sowMinSkill.ToString(),
                "Minimal planting skill needed for sowing",
                500,
                hyperlinks: new Dialog_InfoCard.Hyperlink[]
                {
                new Dialog_InfoCard.Hyperlink(seed.plant)
                });
        }
    }
}