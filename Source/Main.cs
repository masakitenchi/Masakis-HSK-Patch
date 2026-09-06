global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using HarmonyLib;
global using LudeonTK;
global using RimWorld;
global using UnityEngine;
global using Verse;
using System.IO;
using System.Reflection;
using Unity.Collections;

namespace Core_SK_Patch;


public class Main : Mod
{
    private static readonly Dictionary<string, string> CompatAssemblies = new Dictionary<string, string>()
    {
        {"kentington.saveourship2","SOS2Compat" },
        {"notfood.seedsplease", "SeedsPleaseCompat" }
    };

    public static Harmony harmony;

    internal static StringBuilder sb = new StringBuilder("Initializing:\n");

    public static Main instance;

    public Main(ModContentPack content) : base(content)
    {
        if (instance != null) return;
        instance = this;
        this.GetSettings<Settings>();
        harmony = new Harmony("com.reggex.HSKPatch");
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
                Logger.Message("[Core SK Patch]" + sb.ToString());
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
        if (Settings.ErrorChecks)
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                CheckDuplicateComps();
                //CheckBlockingThings();
                CheckNullPlants();
            });
        }
        sb.AppendLine("Initialization Complete");
        Logger.Message(sb.ToString());
    }

    public static void CheckDuplicateComps()
    {
        StringBuilder sb = new StringBuilder();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            if (def.comps is null || def.comps.Count <= 1) continue;
            var set = new HashSet<(Type propType, Type compClass)>();
            foreach (var comp in def.comps)
            {
                var key = (comp.GetType(), comp.compClass);
                if (!set.Add(key))
                {
                    sb.AppendLine($"\t - {comp.GetType().FullName} -> {comp.compClass?.FullName} in {def.defName} ({def.modContentPack?.Name})");
                }
            }
        }
        if (sb.Length > 0) { Logger.Message("[Core SK Patch] Duplicate Comps Found:\n" + sb.ToString()); }
    }

#if !RW_1_5
    public static void CheckBlockingThings()
    {
        StringBuilder sb = new StringBuilder();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(x => !x.generated))
        {
            if (def.preventGravshipLandingOn || (!def.building?.canLandGravshipOn ?? false))
            {
                sb.AppendLine($"\t - {def.defName} ({def.modContentPack?.Name})");
            }
        }
        if (sb.Length > 0) { Logger.Message("[Core SK Patch] Things Blocking Gravship Landing:\n" + sb.ToString()); }
    }
#endif

    public static void CheckNullPlants()
    {
        StringBuilder sb = new StringBuilder();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(x => x.category == ThingCategory.Plant))
        {
            if (def.plant is null)
            {
                sb.AppendLine($"\t - {def.defName} is plant yet has null plantProperties");
            }
            else if (def.plant!.sowTags is null)
            {
                sb.AppendLine($"\t - {def.defName} has null sowTags");
            }
        }
        if (sb.Length > 0) { Logger.Message("[Core SK Patch] Plants with null plantProperties:\n" + sb.ToString()); }
    }

    public override string SettingsCategory() => this.Content.Name;
    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        Listing_Standard ls = new Listing_Standard();
        ls.Begin(inRect);
        ls.CheckboxLabeled("CoreSK_EnableBulkRecipe".Translate(), ref Settings.EnableBulkRecipe, "CoreSK_EnableBulkRecipeTip".Translate());
        Settings.InfestationPreventionRadius = ls.SliderLabeled(
            "CoreSK_InfestationPreventionRadius".Translate(Settings.InfestationPreventionRadius.ToString("F2")),
            Settings.InfestationPreventionRadius,
            10f,
            150f,
            tooltip: "CoreSK_InfestationPreventionRadiusTip".Translate());
        ls.CheckboxLabeled("CoreSK_NeverDieByLowHealth".Translate(), ref Settings.NeverDieByLowHealth, "CoreSK_NeverDieByLowHealthTip".Translate());
        ls.CheckboxLabeled("CoreSK_AutoForbidSpoiled".Translate(), ref Settings.AutoForbidSpoiled, "CoreSK_AutoForbidSpoiledTip".Translate());
        ls.CheckboxLabeled("CoreSK_LogAllPatchOperations".Translate(), ref Settings.LogAllPatchOperations, "CoreSK_LogAllPatchOperationsTip".Translate());
        ls.CheckboxLabeled("CoreSK_ErrorChecks".Translate(), ref Settings.ErrorChecks);
        ls.CheckboxLabeled("CoreSK_FactionDiscovery_Enable".Translate(), ref Settings.EnableFactionDiscovery, "CoreSK_FactionDiscovery_EnableTip".Translate());
        ls.CheckboxLabeled("CoreSK_AllowTamingDownedAnimals".Translate(), ref Settings.AllowTamingDownedAnimals, "CoreSK_AllowTamingDownedAnimalsTip".Translate());
        if (ls.ButtonText("CoreSK_FactionDiscovery_Scan".Translate()))
            FactionDiscoveryUtility.ScanAndPrompt(clearIgnored: true);
        ls.End();
    }


    // Main code borrowed from CE's loader dll
    private bool TryLoadCompatAssembly(string name, out Assembly assembly)
    {
        assembly = null;
        //DirectoryInfo locationInfo = new DirectoryInfo(this.Content.RootDir).GetDirectories("\\AssembliesCompat").FirstOrFallback(null);
        FileInfo assemblyFile = new DirectoryInfo(this.Content.RootDir).GetDirectories(Path.Combine("v" + VersionControl.CurrentVersionStringWithoutBuild, "AssembliesCompat"))?.FirstOrDefault()?.GetFiles(name + ".dll")?.FirstOrDefault();
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
                // Mod discovery already cached the type list before this constructor ran.
                // Include dynamically loaded Def subclasses in the subsequent database registration.
                GenTypes.ClearCache();
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
