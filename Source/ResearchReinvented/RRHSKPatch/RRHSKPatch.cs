using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using static HarmonyLib.Code;
using PeteTimesSix.ResearchReinvented.Rimworld.Comps;
using PeteTimesSix.ResearchReinvented.Rimworld.WorkGivers;
using SK;
using Verse;
using RimWorld;
using PeteTimesSix.ResearchReinvented;

namespace RRHSKPatch;

[StaticConstructorOnStartup]
[HarmonyPatch]
public static class RRHSKPatch
{
    private static readonly MethodInfo GetBestResearchBench = AccessTools.Method(typeof(WorkGiver_Analyse), nameof(WorkGiver_Analyse.GetBestResearchBench));
    private static readonly FieldInfo failCache = AccessTools.Field(typeof(StringsCache), nameof(StringsCache.JobFail_NeedResearchBench));
    static RRHSKPatch()
    {
        Harmony harmony = new Harmony("REGEX.RRHSKPatch");
        Log.Message("RRHSK Patch Inited");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    [HarmonyPatch(typeof(Comp_ResearchKit))]
    [HarmonyPatch("MeetsProjectRequirementsLocally")]
    [HarmonyPostfix]
    public static void Postfix(ref bool __result, ResearchProjectDef project, Comp_ResearchKit __instance)
    {
        if (project.requiredResearchBuilding != null && project.GetModExtension<AdvancedResearchExtension>() != null && project.GetModExtension<AdvancedResearchExtension>().requiredResearchBuildings.Contains(__instance.Props.substitutedResearchBench))
        {
            __result = true;
        }
    }

   /*  [HarmonyPatch(typeof(WorkGiver_Analyse), nameof(WorkGiver_Analyse.HasJobOnThing))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        List<CodeInstruction> inst = new List<CodeInstruction>(instructions);
        int benchIndex = inst.FindIndex(x => x.Is(OpCodes.Call, GetBestResearchBench));
        int stLocIndex = inst[benchIndex + 1].LocalIndex();
        int ldLocIndex = inst.FirstIndexOf(x => x.IsLdloc() && x.LocalIndex() == stLocIndex);
        Label ldsfld = il.DefineLabel();
        inst.First(x => x.LoadsField(failCache)).labels.Add(ldsfld);
        for(int i = 0; i < inst.Count; i++)
        {
            if (i == ldLocIndex)
            {
                yield return inst[i];
                yield return Brfalse[ldsfld];
                yield return inst[i];
                yield return Call[AccessTools.Method(typeof(RRHSKPatch), nameof(IsUsableBench))];
                //yield return inst[i + 1];
                continue;
            }
            else
            {
                yield return inst[i];
            }
        }
    } */

    /// <summary>
    /// Checks if given thing is usable bench for analyze
    /// </summary>
    /// <param name="t">Thing to check</param>
    /// <returns>true if can be used for analyse, otherwise false</returns>
    private static bool IsUsableBench(Thing t)
    {
        if (t is Building_ResearchBench bench) 
        {
            if (!bench.HasComp<CompPowerTrader>() || bench.TryGetComp(out CompPowerLowIdleDraw comp) && !comp.LowPowerMode)
                return true;
        }
        return false;
    }
}
