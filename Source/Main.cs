using System;
using RimWorld;
using Verse;
using HarmonyLib;
using SK.Events;
using RimworldMod;
using System.Collections.Generic;
using System.Text;

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
            StringBuilder sb = new StringBuilder("Core_SK Patch is patching: ");
            Harmony harmony = new Harmony("com.reggex.HSKPatch");
            //SOS2 Compatibility Patch
            if (ModsConfig.IsActive("kentington.saveourship2"))
            {
                sb.Append("Save Our Ship 2\n");
                harmony.Patch(AccessTools.Method(typeof(IncidentWorker_SeismicActivity), "CanFireNowSub"), null, new HarmonyMethod(patchType, "CanFireNowSubPostfix"));
            }
            //Methadone Fix (Disabled in 1.4 since 1.4 has already added Methadone back)
#if ODT
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Hediff), "CurrentStateInternal"), null, new HarmonyMethod(patchType, "MethadoneHigh"));
            harmony.Patch(AccessTools.Method(typeof(AddictionUtility), "CanBingeOnNow"), null, new HarmonyMethod(patchType, "CanBingeOnNowPostfix"));
            sb.Append("Methadone Fix\n");
#endif
            harmony.PatchAll();
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
    }
}
