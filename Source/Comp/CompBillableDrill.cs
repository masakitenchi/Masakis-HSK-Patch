namespace Core_SK_Patch;


public class CompBillableDrill : ThingComp
{
	public CompDeepDrill deepDrill;
	public ThingDef currentResource;
	private int _billableResourceLimit = -1;
	public int BillableResourceLimit => _billableResourceLimit;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		this.deepDrill = this.parent.TryGetComp<CompDeepDrill>();
		this.currentResource = DeepDrillUtility.GetNextResource(this.parent.Position, this.parent.Map);
	}

	public override string CompInspectStringExtra()
	{
		if (this._billableResourceLimit != -1)
		{
			return "CSP_CurrentLimit".Translate(this._billableResourceLimit);
		}
		return "";
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (this.deepDrill is null)
		{
			yield break;
		}
		yield return new Command_Action()
		{
			defaultLabel = "CSP_BillableDrillLimit".Translate(),
			defaultDesc = "CSP_BillableDrillDesc".Translate(this.currentResource),
			icon = this.currentResource.uiIcon,
			action = delegate
			{
				Find.WindowStack.Add(new Dialog_Slider((current) => "CSP_CurrentLimit".Translate(current), -1, 5000, delegate (int value)
				{
					foreach (Thing drill in parent.Map.listerBuildings.AllBuildingsColonistOfDef(parent.def))
					{
						CompBillableDrill compBillableDrill = drill.TryGetComp<CompBillableDrill>();
						if (compBillableDrill.currentResource == this.currentResource)
							compBillableDrill.Nofity_LimitSelected(value);
					}
				}, this._billableResourceLimit));
			},
			groupable = true,
			groupKey = this.currentResource.GetHashCode() * 0x114514,

		};
	}

	public void Nofity_LimitSelected(int value)
	{
		this._billableResourceLimit = value;
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref this._billableResourceLimit, "billableResourceLimit", -1);
	}
}

[HarmonyPatch]
public static class BillableDrillPatch
{
	[HarmonyPatch(typeof(CompDeepDrill), nameof(CompDeepDrill.CanDrillNow))]
	[HarmonyPostfix]
	public static void Check(CompDeepDrill __instance, ref bool __result)
	{
		if (__instance.parent.TryGetComp<CompBillableDrill>() is CompBillableDrill compBillableDrill)
		{
			if (compBillableDrill.BillableResourceLimit == -1) return;
			Map map = compBillableDrill.parent.Map;
			if (Find.CurrentMap.listerThings.listsByDef[compBillableDrill.currentResource].Sum(x => x.stackCount) >= compBillableDrill.BillableResourceLimit)
			{
				__result = false;
			}
		}
	}
}


