global using HarmonyLib;
global using System.Collections.Generic;
global using SeedsPlease;
global using RimWorld;
global using Verse;
global using Core_SK_Patch;
using System.Linq;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class SeedPatch
{
    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
    [HarmonyPostfix]
    public static IEnumerable<StatDrawEntry> AddSkillRequirement(IEnumerable<StatDrawEntry> results, ThingDef __instance)
    {
        foreach (var entry in results)
        {
            yield return entry;
        }
        if (__instance is SeedDef seed)
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsImportant, "LblSkill".Translate(),
                seed.plant.plant.sowMinSkill.ToString(),
                "LblSkillDesc".Translate(),
                500,
                hyperlinks: new Dialog_InfoCard.Hyperlink[]
                {
                new Dialog_InfoCard.Hyperlink(seed.plant)
                });
            if (!seed.plant.plant.sowResearchPrerequisites.NullOrEmpty())
                yield return new StatDrawEntry(
                   StatCategoryDefOf.BasicsImportant, "LblTech".Translate(),
                    string.Empty,
                    "LblTechDesc".Translate(),
                    501,
                    hyperlinks: seed.plant.plant.sowResearchPrerequisites.Select(x => new Dialog_InfoCard.Hyperlink(x))
                    );
        }
    }
}