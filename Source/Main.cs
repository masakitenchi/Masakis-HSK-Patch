using System;
using RimWorld;
using Verse;
using HarmonyLib;
using SK.Events;
using RimworldMod;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Core_SK_Patch
{
#if ODT
    [DefOf]
    public static class HediffDefOfLocal
    {
        public static HediffDef MethadoneHigh;

        static HediffDefOfLocal() => DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOfLocal)); 
    }
#endif

    [StaticConstructorOnStartup]
    public static class Core_SK_Patch
    {
        private static readonly Type patchType;
        static Core_SK_Patch()
        {
#if DEBUG
			Harmony.DEBUG = true;
#endif
            patchType = typeof(Core_SK_Patch);
            StringBuilder sb = new StringBuilder("Core_SK Patch is patching:\n");
            Harmony harmony = new Harmony("com.reggex.HSKPatch");
            //SOS2 Compatibility Patch
            if (ModsConfig.IsActive("kentington.saveourship2"))
            {
                harmony.Patch(AccessTools.Method(typeof(IncidentWorker_SeismicActivity), "CanFireNowSub"), null, new HarmonyMethod(patchType, "CanFireNowSubPostfix"));
                sb.Append(" - Save Our Ship 2 Patched");
            }
            //Methadone Fix (Disabled in 1.4 since 1.4 has already added Methadone back)
#if ODT
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Hediff), "CurrentStateInternal"), null, new HarmonyMethod(patchType, "MethadoneHigh"));
            harmony.Patch(AccessTools.Method(typeof(AddictionUtility), "CanBingeOnNow"), null, new HarmonyMethod(patchType, "CanBingeOnNowPostfix"));
            sb.AppendLine("Methadone Fix (1.3 only)");
#endif
            harmony.PatchAll();
            sb.AppendLine(" - Generating Recipes for:");
            int count = 0;
            List<RecipeDef> RecipesToAdd = new();
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs.Where(x => x.HasModExtension<ModExtension_BulkRecipe>()))
            {
                sb.AppendLine("  - " + recipe.defName);
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
        public static void CanFireNowSubPostfix(ref bool __result, IncidentParms parms)
        {
            Map obj = (Map)parms.target;
            __result = !obj.IsSpace() && __result;
        }



        #region Bulk Recipe
        static RecipeDef GenerateBulkRecipe(ModExtension_BulkRecipe ModExt, RecipeDef Recipe)
        {
            RecipeDef BulkRecipe = new RecipeDef();
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
}
