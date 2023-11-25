global using System;
global using RimWorld;
global using Verse;
global using HarmonyLib;
global using UnityEngine;
global using System.Collections.Generic;
global using System.Text;
global using System.Linq;
using System.IO;
using System.Reflection;

namespace Core_SK_Patch;

#if ODT
[DefOf]
public static class HediffDefOfLocal
{
    public static HediffDef MethadoneHigh;

    static HediffDefOfLocal() => DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOfLocal)); 
}
#endif

public class Main : Mod
{
    private static readonly Dictionary<string, string> CompatAssemblies = new Dictionary<string, string>()
    {
        {"kentington.saveourship2","SOS2Compat" },
        {"notfood.seedsplease", "SeedsPleaseCompat" }
    };

    public static Harmony harmony;

    internal static StringBuilder sb = new StringBuilder("Initializing:\n");


    public Main(ModContentPack content) : base(content)
    {
        harmony = new Harmony("com.reggex.HSKPatch");
#if ODT
        //Methadone Fix (Disabled in 1.4 since 1.4 has already added Methadone back)
        harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Hediff), "CurrentStateInternal"), null, new HarmonyMethod(patchType, "MethadoneHigh"));
        harmony.Patch(AccessTools.Method(typeof(AddictionUtility), "CanBingeOnNow"), null, new HarmonyMethod(patchType, "CanBingeOnNowPostfix"));
        sb.AppendLine(" - Methadone Fix (1.3 only)");
#endif
        if (Settings.EnableBulkRecipe)
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                StringBuilder sb = new StringBuilder();
                int count = 0;
                List<RecipeDef> RecipesToAdd = new();
                foreach (var recipe in DefDatabase<RecipeDef>.AllDefs.Where(x => x.HasModExtension<ModExtension_BulkRecipe>()))
                {
                    var ModExt = recipe.GetModExtension<ModExtension_BulkRecipe>();
                    if (ModExt.MaterialMultiplier == 1 && ModExt.ProductMultiplier == 1 && ModExt.WorkAmountMultiplier == 1)
                        continue;
                    var BulkRecipe = GenerateBulkRecipe(ModExt, recipe);
                    BulkRecipe.ResolveReferences();
                    RecipesToAdd.Add(BulkRecipe);
                    ++count;
                }
                count = 0;
                if (RecipesToAdd.Count > 0)
                {
                    StringBuilder Tuples = new StringBuilder();
                    for (int i = 0; i < RecipesToAdd.Count; i++)
                    {
                        DefGenerator.AddImpliedDef<RecipeDef>(RecipesToAdd[i]);
                        if (RecipesToAdd[i].recipeUsers != null)
                        {
                            for (int j = 0; j < RecipesToAdd[i].recipeUsers.Count; j++)
                            {
                                RecipesToAdd[i].recipeUsers[j].AllRecipes.Add(RecipesToAdd[i]);
                                Tuples.AppendLine("  - " + RecipesToAdd[i].defName + " to " + RecipesToAdd[i].recipeUsers[j].defName);
                            }
                        }
                        else
                        {
                            foreach (var thing in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.recipes != null && x.recipes.Any(y => RecipesToAdd[i].defName.Contains(y.defName))))
                            {
                                thing.AllRecipes.Add(RecipesToAdd[i]);
                                Tuples.AppendLine("  - " + RecipesToAdd[i].defName + " to " + thing.defName);
                            }
                        }
                        ++count;
                    }
                    sb.AppendLine($" - Added {count} bulk recipes :");
                    sb.Append(Tuples);
                }
                Log.Message("[Core SK Patch]" + sb.ToString());
            }, "BulkRecipe", false, null);
        }
        sb.AppendLine(" - Loading Compat dlls:");
        foreach (var mod in CompatAssemblies)
        {
            if (ModsConfig.IsActive(mod.Key))
            {
                if (TryLoadCompatAssembly(mod.Value, out var assembly))
                {
                    LongEventHandler.QueueLongEvent(() => harmony.PatchAll(assembly), "SOS2Patch", false, null);
                }

            }
        }
        harmony.PatchAll();
        //harmony.Unpatch(AccessTools.Method(typeof(RegionTypeUtility), nameof(RegionTypeUtility.GetExpectedRegionType)), HarmonyPatchType.All, "skyarkhangel.HSK");
        /*
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            DefDatabase<ThingDef>.AllDefs.AsParallel().ForAll(x =>            {
                if (x.IsBuildingArtificial && !x.IsFrame && x.selectable && x.useHitPoints && x.statBases is null) Log.Warning($"{x.defName} is building with null statBases");
            });
        });*/
        sb.AppendLine("Initialization Complete");
        Log.Message(sb.ToString());
    }
#if ODT
    public static void MethadoneHigh(ThoughtWorker_Hediff __instance, ref ThoughtState __result, Pawn p)
    {
        if (__result.StageIndex != ThoughtState.Inactive.StageIndex)
        {
            //Hediff firstHediffOfDef = p.health.hediffSet.GetFirstHediffOfDef(__instance.def.hediff);
            if (__instance.def.defName.Contains("Withdrawal") && p.health.hediffSet.HasHediff(HediffDefOfLocal.MethadoneHigh))
            {
                __result = ThoughtState.Inactive;
            }
        }
    }
    public static void CanBingeOnNowPostfix(ref bool __result, Pawn pawn)
    {
        if (pawn.health.hediffSet.HasHediff(HediffDefOfLocal.MethadoneHigh))
            __result = false;
    }
#endif

    public override string SettingsCategory() => this.Content.Name;
    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        Listing_Standard ls = new Listing_Standard();
        ls.Begin(inRect);
        ls.CheckboxLabeled("Enable Bulk Recipe", ref Settings.EnableBulkRecipe, "Check to generate bulk recipes for patched recipes (Needs restart)");
        Settings.InfestationPreventionRadius = ls.SliderLabeled($"Deep Infestation Radius : {Settings.InfestationPreventionRadius:F2}", Settings.InfestationPreventionRadius, 10f, 150f, tooltip: "If one deep drill has an infestation recently, it will prevent all deep drills in a certain radius from being infested again. This slider lets you change how big that circle is");
        ls.CheckboxLabeled("Never die by low health", ref Settings.NeverDieByLowHealth, "Removes the health check for death.\n(Need restart after changing the value)");
        ls.CheckboxLabeled("Auto-forbid rotten mush & meat out of home area", ref Settings.AutoForbidSpoiled, "Automatically forbid rotten mush & meat generated outside of home area. No more pawn walking across the whole map just to get 1 pile of rotten mush to your storage");
        ls.End();
    }


    // Main code borrowed from CE's loader dll
    private bool TryLoadCompatAssembly(string name, out Assembly assembly)
    {
        assembly = null;
        //DirectoryInfo locationInfo = new DirectoryInfo(this.Content.RootDir).GetDirectories("\\AssembliesCompat").FirstOrFallback(null);
        FileInfo assemblyFile = new DirectoryInfo(this.Content.RootDir).GetDirectories("AssembliesCompat")?.FirstOrDefault()?.GetFiles(name + ".dll")?.First();
        if (assemblyFile is not null)
        {
            byte[] rawAssembly = File.ReadAllBytes(assemblyFile.FullName);
            /*FileInfo pdbFile = new FileInfo(Path.Combine(assemblyFile.DirectoryName, Path.GetFileNameWithoutExtension(assemblyFile.FullName)) + ".pdb");
            if (pdbFile.Exists)
            {
                assembly = AppDomain.CurrentDomain.Load(rawAssembly, File.ReadAllBytes(pdbFile.FullName));
            }
            else
            {*/
            assembly = AppDomain.CurrentDomain.Load(rawAssembly);
            //}
            if (assembly != null)
            {
                Content.assemblies.loadedAssemblies.Add(assembly);
                sb.AppendLine(" - " + assembly.FullName);
                return true;
            }
        }
        return false;
    }
    #region Bulk Recipe
    static RecipeDef GenerateBulkRecipe(ModExtension_BulkRecipe ModExt, RecipeDef Recipe)
    {
        RecipeDef BulkRecipe = new();
        DeepCopy(BulkRecipe, Recipe);
        // Adjust the values based on the mod extension
        foreach (var ingredient in Recipe.ingredients)
        {
            var batchIngredient = new IngredientCount();
            batchIngredient.SetBaseCount(ingredient.GetBaseCount() * ModExt.MaterialMultiplier);
            batchIngredient.filter = ingredient.filter;
            BulkRecipe.ingredients.Add(batchIngredient);
        }
        foreach (var product in Recipe.products)
        {
            var batchProduct = new ThingDefCountClass();
            batchProduct.thingDef = product.thingDef;
            batchProduct.count = ((int)Math.Ceiling(product.count * ModExt.ProductMultiplier));
            BulkRecipe.products.Add(batchProduct);
        }
        BulkRecipe.workAmount = Recipe.workAmount * ModExt.WorkAmountMultiplier;
        return BulkRecipe;
    }

    static void DeepCopy(RecipeDef recipient, RecipeDef original)
    {
        recipient.defName = original.defName + "_Bulk";
        recipient.label = original.label + "BulkRecipe".Translate();
        recipient.description = original.description;
        recipient.jobString = original.jobString;
        recipient.effectWorking = original.effectWorking;
        recipient.soundWorking = original.soundWorking;
        recipient.workSkill = original.workSkill;
        recipient.workSkillLearnFactor = original.workSkillLearnFactor;
        recipient.workSpeedStat = original.workSpeedStat;
        recipient.fixedIngredientFilter = original.fixedIngredientFilter;
        recipient.skillRequirements = original.skillRequirements;
        recipient.recipeUsers = original.recipeUsers;
        recipient.researchPrerequisite = original.researchPrerequisite;
        recipient.researchPrerequisites = original.researchPrerequisites;
    }
    #endregion
}
