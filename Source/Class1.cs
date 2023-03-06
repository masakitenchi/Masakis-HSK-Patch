using System;
using RimWorld;
using Verse;
using HarmonyLib;
using SK.Events;
using RimworldMod;
using System.Collections.Generic;

namespace Core_SK_Patch
{
	[DefOf]
	public static class HediffDefOfLocal
    {
		public static HediffDef MethadoneHigh;
		static HediffDefOfLocal()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOfLocal));
		}
	}
	[StaticConstructorOnStartup]
	public static class Core_SK_Patch
	{
		private static readonly Type patchType;
		static Core_SK_Patch()
        {
#if DEBUG
			Harmony.DEBUG = true;
#endif
			Log.Message("CoreSKPatch Enabled");
			patchType = typeof(Core_SK_Patch);
			Harmony harmony = new Harmony("com.reggex.HSKPatch");
			//SOS2 Compatibility Patch
			if(ModsConfig.IsActive("kentington.saveourship2"))
            {
				Log.Message("CoreSK_SOS2Patch Enabled");
				harmony.Patch(AccessTools.Method(typeof(IncidentWorker_SeismicActivity), "CanFireNowSub"), null, new HarmonyMethod(patchType, "CanFireNowSubPostfix"));
            }
			//Methadone Fix
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Hediff), "CurrentStateInternal"), null, new HarmonyMethod(patchType, "MethadoneHigh"));
			harmony.Patch(AccessTools.Method(typeof(AddictionUtility), "CanBingeOnNow"), null, new HarmonyMethod(patchType, "CanBingeOnNowPostfix"));
			//harmony.Patch(AccessTools.Method(typeof(DebugActionsUtility), "PointsOptions"), postfix: new HarmonyMethod(patchType, "PointsPostfix"));
			Log.Message("MethadoneFix Enabled");
			//harmony.Patch(AccessTools.Method(typeof(SurvivalToolUtility), "CanUseSurvivalTools"), null, new HarmonyMethod(patchType, "CanUseToolsPostfix"));
			
        }
		/* Currently need some new conditions since Friendly Mechanoid's been added since Biotech
		public static void CanUseToolsPostfix(ref bool __result, Pawn pawn)
        {
			__result=pawn.def.HasModExtension<MechanicalPawnProperties>() ? false : __result;
			return ;
        }*/
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
		public static void CanFireNowSubPostfix(ref bool __result, IncidentParms parms)
        {
			Map obj = (Map)parms.target;
			__result = !obj.IsSpace() && __result;
        }
    }
}
