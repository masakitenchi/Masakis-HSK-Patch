using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Core_SK_Patch;


[HarmonyPatch]
public static class ShuttleStuffPatch
{
    //Add a new comp CompShuttleStuff to store shuttle stuff info.
    //When shuttle transform into pawn, the pawn will save the stuff info into CompShuttleStuff.stuff.
    //When shuttle pawn transform into building, the building will use the stuff stored in the pawn's comp to determine its stuff.

    //Seems that optimization in compiling will result in local variables being dislocated (My dll only has a Pawn loc.0, while github ver has a PawnKindDef loc.0 and a Pawn loc.1)

    /*  public static Pawn myPawn(Thing meAsABuilding, IntVec3 myPos, int fuelAmount)
	 *  
		 * // pawn.GetComp<CompBecomeBuilding>().stuff = meAsABuilding.Stuff;
		IL_005e: ldloc.0
		IL_005f: callvirt instance !!0 ['Assembly-CSharp']Verse.ThingWithComps::GetComp<class Core_SK_Patch.CompBecomeBuilding>()
		IL_0064: ldarg.0
		IL_0065: callvirt instance class ['Assembly-CSharp']Verse.ThingDef ['Assembly-CSharp']Verse.Thing::get_Stuff()
		IL_006a: stfld class ['Assembly-CSharp']Verse.ThingDef Core_SK_Patch.CompBecomeBuilding::stuff
	*/
    private static readonly MethodInfo TryGetComp = AccessTools.Method(typeof(ThingCompUtility), nameof(ThingCompUtility.TryGetComp), parameters: [typeof(Thing)], generics: [typeof(CompShuttleStuff)]);
    private static readonly MethodInfo get_Stuff = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Stuff));

    private static readonly FieldInfo CompStuff = AccessTools.Field(typeof(CompShuttleStuff), nameof(CompShuttleStuff.stuff));
    private static readonly FieldInfo pawn_apparel = AccessTools.Field(typeof(Pawn), nameof(Pawn.apparel));
    private static readonly FieldInfo parent = AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent));

    [HarmonyPrepare]
    public static bool Prepare()
    {
        foreach (var field in typeof(ShuttleStuffPatch).GetFields())
        {
            if (field.GetValue(null) == null)
                Logger.Error($"Core_SK_Patch :: Patch class {nameof(ShuttleStuffPatch)} has null field {field.Name}. This may lead to failed patch.");
        }
        return true;
    }
    [HarmonyPatch(typeof(CompBecomePawn), nameof(CompBecomePawn.myPawn))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> PawnTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        bool inserted = false;
        List<CodeInstruction> inst = instructions.ToList();
        for (var i = 0; i < inst.Count; i++)
        {
            if (!inserted && inst[i].Is(OpCodes.Stfld, pawn_apparel))
            {
                inst.InsertRange(i + 1, new CodeInstruction[]
                {
                    new (inst[i-3]),  // Make sure we're getting the pawn variable, not the PawnKindDef
					new (OpCodes.Callvirt, TryGetComp),
                    new (OpCodes.Ldarg_0),
                    new (OpCodes.Callvirt, get_Stuff),
                    new (OpCodes.Stfld, CompStuff)
                });
                inserted = true;
                break;
            }
        }
        //File.WriteAllText("E:\\after.txt", string.Join("\n", inst.Select(x => x.ToString())));
        if (!inserted)
        {
            Logger.Error($"Core_SK_Patch :: Cannot Patch CompBecomePawn.myPawn");
            return instructions;
        }
        return inst;
    }


    private static readonly MethodInfo MakeThing = AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing));
    /*// Building building = (Building)ThingMaker.MakeThing(Props.buildingDef);
	IL_003d: ret

	IL_003e: ldarg.0
	IL_003f: call instance class RimWorld.CompProperties_BecomeBuilding RimWorld.CompBecomeBuilding::get_Props()
	IL_0044: ldfld class ['Assembly-CSharp']Verse.ThingDef RimWorld.CompProperties_BecomeBuilding::buildingDef
	IL_0049: ldnull
	IL_004a: call class ['Assembly-CSharp']Verse.Thing ['Assembly-CSharp']Verse.ThingMaker::MakeThing(class ['Assembly-CSharp']Verse.ThingDef, class ['Assembly-CSharp']Verse.ThingDef)
	IL_004f: castclass ['Assembly-CSharp']Verse.Building
	IL_0054: stloc.2
	*/
    [HarmonyPatch(typeof(CompBecomeBuilding), nameof(CompBecomeBuilding.transform))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> BuildingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        int idnull = -1;
        List<CodeInstruction> inst = instructions.ToList();
        for (var i = 0; i < inst.Count; i++)
        {
            if (inst[i].opcode == OpCodes.Ldnull && inst[i + 1].Is(OpCodes.Call, MakeThing))
            {
                // new CompBecomeBuilding().parent.TryGetComp<CompShuttleStuff>().stuff

                inst[i] = new CodeInstruction(OpCodes.Ldarg_0); // CompBecomeBuilding instance
                inst.InsertRange(i + 1, new CodeInstruction[]
                {
                    new (OpCodes.Ldfld, parent),
                    new (OpCodes.Callvirt, TryGetComp),
                    new (OpCodes.Ldfld, CompStuff)
                });
                idnull = i;
                break;
            }
        }
        //File.WriteAllText("E:\\after1.txt", string.Join("\n", inst.Select(x => x.ToString())));
        if (idnull == -1)
        {
            Logger.Error("Core_SK_Patch :: Cannot Patch CompBecomeBuilding.transform");
            return instructions;
        }
        return inst;
    }


}
