using System.Reflection;
using System.Reflection.Emit;
using RimWorld.QuestGen;

namespace Core_SK_Patch;
[HarmonyPatch]
public static class SmarterShuttleRescue
{
	private static readonly MethodInfo _defendPoint = AccessTools.Method(typeof(QuestGen_Lord), nameof(QuestGen_Lord.DefendPoint));
	//RimWorld.QuestGen.QuestNode_Root_ShuttleCrash_Rescue+<>c__DisplayClass16_0.<RunInt>b__0
	/*
		[CompilerGenerated]
		public sealed class <>c__DisplayClass16_0
		{
			public Quest quest;
			public Action <>9__7;
			public Action <>9__6;
			public Action <>9__4;
			public string questSuccess;
			public float questPoints;
			public Faction enemyFaction;
			public QuestNode_Root_ShuttleCrash_Rescue <>4__this;
			public Pawn asker;
			public List<Pawn> civilians;
			public List<Pawn> soldiers;
			public List<Pawn> allPassengers;
			public IntVec3 shuttleCrashPosition;
			public Map map;
			public Thing crashedShuttle;
			public Thing rescueShuttle;

			public <>c__DisplayClass16_0();

			public void <RunInt>b__0();
			public void <RunInt>b__1();
			public void <RunInt>b__2();
			public void <RunInt>b__3();
			public void <RunInt>b__4();
			public void <RunInt>b__6();
			public void <RunInt>b__7();
		}
	*/
	// We need to move this questPart to a later stage, after we have calculated the position
	/*
		// quest.DefendPoint(map.Parent, shuttleCrashPosition, soldiers, Faction.OfEmpire, null, null, 12f, isCaravanSendable: false, addFleeToil: false);
		IL_00cd: pop
		IL_00ce: ldarg.0
		IL_00cf: ldfld class RimWorld.Quest RimWorld.QuestGen.QuestNode_Root_ShuttleCrash_Rescue/'<>c__DisplayClass16_0'::quest
		IL_00d4: ldarg.0
		IL_00d5: ldfld class Verse.Map RimWorld.QuestGen.QuestNode_Root_ShuttleCrash_Rescue/'<>c__DisplayClass16_0'::map
		IL_00da: callvirt instance class RimWorld.Planet.MapParent Verse.Map::get_Parent()
		IL_00df: ldarg.0
		IL_00e0: ldfld valuetype Verse.IntVec3 RimWorld.QuestGen.QuestNode_Root_ShuttleCrash_Rescue/'<>c__DisplayClass16_0'::shuttleCrashPosition
		IL_00e5: ldarg.0
		IL_00e6: ldfld class [mscorlib]System.Collections.Generic.List`1<class Verse.Pawn> RimWorld.QuestGen.QuestNode_Root_ShuttleCrash_Rescue/'<>c__DisplayClass16_0'::soldiers
		IL_00eb: call class RimWorld.Faction RimWorld.Faction::get_OfEmpire()
		IL_00f0: ldnull
		IL_00f1: ldnull
		IL_00f2: ldc.i4.s 12
		IL_00f4: conv.r4
		IL_00f5: newobj instance void valuetype [mscorlib]System.Nullable`1<float32>::.ctor(!0)
		IL_00fa: ldc.i4.0
		IL_00fb: ldc.i4.0
		IL_00fc: call class RimWorld.QuestPart_DefendPoint RimWorld.QuestGen.QuestGen_Lord::DefendPoint(class RimWorld.Quest, class RimWorld.Planet.MapParent, valuetype Verse.IntVec3, class [mscorlib]System.Collections.Generic.IEnumerable`1<class Verse.Pawn>, class RimWorld.Faction, string, string, valuetype [mscorlib]System.Nullable`1<float32>, bool, bool)
		IL_0101: pop
	*/
	/*
		// IntVec3 position = shuttleCrashPosition + IntVec3.South;
		IL_0102: ldarg.0
		IL_0103: ldfld valuetype Verse.IntVec3 RimWorld.QuestGen.QuestNode_Root_ShuttleCrash_Rescue/'<>c__DisplayClass16_0'::shuttleCrashPosition
		IL_0108: ldsfld valuetype Verse.IntVec3 Verse.IntVec3::South
		IL_010d: call valuetype Verse.IntVec3 Verse.IntVec3::op_Addition(valuetype Verse.IntVec3, valuetype Verse.IntVec3)
		IL_0112: stloc.1
	*/
	// If there's an available beacon, move to that position
	/* if (DropCellFinder.TryFindShipLandingArea(map, out IntVec3 result, out Thing _))
		{
			position = result;
		}
	*/


}