using RimWorld;
using Verse;
using HarmonyLib;

namespace Temp_MechJobFix
{
    [StaticConstructorOnStartup]
    public class MechJobFix
    {
        static MechJobFix()
        {
            if (!(ModLister.BiotechInstalled && ModsConfig.IsActive("dubwise.rimefeller")))
                return;
            Harmony harmony = new Harmony("SK.MechJobFix");
            System.Type[] types =
            {
                typeof(Rimefeller.WorkGiver_OperateResourceConsole),
                typeof(Rimefeller.WorkGiver_SuperviseDrilling)
            };
            string methodName = "HasJobOnThing";
            foreach(var type in types)
            {
                harmony.Patch(AccessTools.Method(type, methodName),prefix: new HarmonyMethod(typeof(MechJobFix),"ConsolePrefix"));
            }
            Log.Message("Temp-MechJobFix Inited");
        }
        [HarmonyPrefix]
        public static bool ConsolePrefix(ref bool __result, Pawn pawn)
        {
            if(pawn.IsColonyMech)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
