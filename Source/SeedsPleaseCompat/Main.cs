global using System.Collections.Generic;
global using Core_SK_Patch;
global using HarmonyLib;
global using RimWorld;
global using SeedsPlease;
global using Verse;
using System.Linq;

namespace Core_SK_Patch;

[StaticConstructorOnStartup]
[HarmonyPatch]
public static class SeedPatch
{
    public static Dictionary<string, HashSet<ThingDef>> sowTags = new Dictionary<string, HashSet<ThingDef>>();

    static SeedPatch()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.building is not BuildingProperties building || !building.SupportsPlants) continue;
                if (!sowTags.TryGetValue(building.sowTag, out var buildings))
                {
                    buildings = new();
                    sowTags.Add(building.sowTag, buildings);
                }
                buildings.Add(def);
            }

        });
    }

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
            if (seed.plant.plant.mustBeWildToSow)
            {
                var sowableBiomes = DefDatabase<BiomeDef>.AllDefsListForReading
                    .Where(biome => biome.CommonalityOfPlant(seed.plant) > 0f)
                    .ToList();
                if (!sowableBiomes.NullOrEmpty())
                {
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.BasicsImportant, "LblBiomes".Translate(),
                        sowableBiomes.Select(x => x.LabelCap).Join(),
                        "LblBiomeDesc".Translate(),
                        502,
                        hyperlinks: sowableBiomes.Select(x => new Dialog_InfoCard.Hyperlink(x))
                    );
                }
            }
            if (seed.plant.plant.sowTags is List<string> sowTags)
            {
                if (!sowTags.Exists(x => x == "Ground"))
                {
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.BasicsImportant, "LblSowTags".Translate(),
                        string.Empty,
                        "LblSowTagsDesc".Translate(),
                        503,
                        hyperlinks: sowTags.SelectMany(tag =>
                        {
                            return SeedPatch.sowTags.TryGetValue(tag, out var buildings)
                                    ? buildings.Select(def => new Dialog_InfoCard.Hyperlink(def))
                                    : Enumerable.Empty<Dialog_InfoCard.Hyperlink>();
                        })
                    );
                }
            }
        }
    }
}
