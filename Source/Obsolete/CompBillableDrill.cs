namespace Core_SK_Patch;


public class CompBillableDrill : ThingComp
{
    public CompDeepDrill deepDrill;
    private ThingDef _selectedResource;
    private int _billableResourceLimit = -1;
    public int BillableResourceLimit => _billableResourceLimit;
    public bool ResourceSelectionUnlocked => CSPResearchDefOf.CSP_DeepDrillBulkResources.IsFinished;
    public ThingDef SelectedAlternativeResource => ResourceSelectionUnlocked && IsAlternativeResource(_selectedResource) ? _selectedResource : null;
    public ThingDef CurrentResource => SelectedAlternativeResource ?? DeepDrillUtility.GetNextResource(this.parent.Position, this.parent.Map);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        this.deepDrill = this.parent.TryGetComp<CompDeepDrill>();
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder result = new StringBuilder();
        ThingDef currentResource = this.CurrentResource;
        if (this.ResourceSelectionUnlocked && currentResource is not null)
        {
            result.Append("CSP_DrillResourceInspect".Translate(currentResource));
        }
        if (this._billableResourceLimit != -1)
        {
            if (result.Length > 0)
                result.AppendLine();
            result.Append("CSP_CurrentLimit".Translate(this._billableResourceLimit));
        }
        return result.ToString();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (this.deepDrill is null)
        {
            yield break;
        }

        if (this.ResourceSelectionUnlocked)
        {
            ThingDef selectedResource = this.SelectedAlternativeResource;
            ThingDef displayedResource = selectedResource ?? DeepDrillUtility.GetNextResource(this.parent.Position, this.parent.Map);
            yield return new Command_Action()
            {
                defaultLabel = "CSP_DrillResourceMode".Translate(selectedResource?.LabelCap ?? "CSP_DrillResourceMinerals".Translate()),
                defaultDesc = "CSP_DrillResourceModeDesc".Translate(),
                icon = displayedResource?.uiIcon,
                action = delegate
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("CSP_DrillResourceMinerals".Translate(), () => this.SelectResource(null)),
                        new FloatMenuOption(CSPThingDefOf.SandResource.LabelCap, () => this.SelectResource(CSPThingDefOf.SandResource)),
                        new FloatMenuOption(CSPThingDefOf.CrushedStone.LabelCap, () => this.SelectResource(CSPThingDefOf.CrushedStone))
                    };
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }

        ThingDef currentResource = this.CurrentResource;
        if (currentResource is null)
        {
            yield break;
        }
        yield return new Command_Action()
        {
            defaultLabel = "CSP_BillableDrillLimit".Translate(),
            defaultDesc = "CSP_BillableDrillDesc".Translate(currentResource),
            icon = currentResource.uiIcon,
            action = delegate
            {
                Find.WindowStack.Add(new Dialog_Slider((current) => "CSP_CurrentLimit".Translate(current), -1, 5000, delegate (int value)
                {
                    foreach (Thing drill in parent.Map.listerBuildings.AllBuildingsColonistOfDef(parent.def))
                    {
                        CompBillableDrill compBillableDrill = drill.TryGetComp<CompBillableDrill>();
                        if (compBillableDrill?.CurrentResource == currentResource)
                            compBillableDrill.Nofity_LimitSelected(value);
                    }
                }, this._billableResourceLimit));
            },
            groupable = true,
            groupKey = currentResource.GetHashCode() * 0x114514,
        };
    }

    private static bool IsAlternativeResource(ThingDef resource)
    {
        return resource == CSPThingDefOf.SandResource || resource == CSPThingDefOf.CrushedStone;
    }

    private void SelectResource(ThingDef resource)
    {
        this._selectedResource = resource;
    }

    public void Nofity_LimitSelected(int value)
    {
        this._billableResourceLimit = value;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref this._billableResourceLimit, "billableResourceLimit", -1);
        Scribe_Defs.Look(ref this._selectedResource, "selectedDrillResource");
    }
}

[DefOf]
public static class CSPResearchDefOf
{
    public static ResearchProjectDef CSP_DeepDrillBulkResources;

    static CSPResearchDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CSPResearchDefOf));
    }
}

[DefOf]
public static class CSPThingDefOf
{
    public static ThingDef SandResource;
    public static ThingDef CrushedStone;

    static CSPThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CSPThingDefOf));
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
            if (compBillableDrill.SelectedAlternativeResource is not null)
            {
                CompPowerTrader powerComp = __instance.parent.TryGetComp<CompPowerTrader>();
                __result = powerComp is null || powerComp.PowerOn;
            }

            if (!__result || compBillableDrill.BillableResourceLimit == -1) return;
            Map map = compBillableDrill.parent.Map;
            ThingDef currentResource = compBillableDrill.CurrentResource;
            if (currentResource is not null && map.listerThings.ThingsOfDef(currentResource).Sum(x => x.stackCount) >= compBillableDrill.BillableResourceLimit)
            {
                __result = false;
            }
        }
    }
}

[HarmonyPatch(typeof(CompDeepDrill), "GetNextResource")]
public static class SelectableDeepDrillResourcePatch
{
    [HarmonyPrefix]
    public static bool UseSelectedResource(CompDeepDrill __instance, ref ThingDef __0, ref int __1, ref IntVec3 __2, ref bool __result)
    {
        ThingDef selectedResource = __instance.parent.TryGetComp<CompBillableDrill>()?.SelectedAlternativeResource;
        if (selectedResource is null)
        {
            return true;
        }

        __0 = selectedResource;
        __1 = int.MaxValue;
        __2 = __instance.parent.Position;
        __result = false;
        return false;
    }
}


