namespace Core_SK_Patch;


public class CompAutoExtractorLimit : ThingComp
{
    private int _resourceLimit = -1;

    public ThingDef CurrentResource
    {
        get
        {
            if (!this.parent.Spawned)
            {
                return null;
            }

            ThingDef resource;
            int countPresent;
            IntVec3 cell;
            if (this.parent is SK.Building_Extractor extractor)
            {
                int cells = this.parent.def.GetModExtension<SK.MineExtractorExtension>()?.MineSizeinCells ?? 9;
                SK.Building_Extractor.GetNextResource(extractor.Position, extractor.Map, cells, out resource, out countPresent, out cell);
                return resource;
            }

            if (this.parent is SK.Building_AdvancedExtractor advancedExtractor)
            {
                int cells = this.parent.def.GetModExtension<SK.MineAdvancedExtractorExtension>()?.MineSizeinCells ?? 16;
                SK.Building_AdvancedExtractor.GetNextResource(advancedExtractor.Position, advancedExtractor.Map, cells, out resource, out countPresent, out cell);
                return resource;
            }

            return null;
        }
    }

    public override string CompInspectStringExtra()
    {
        return this._resourceLimit == -1 ? "" : "CSP_CurrentLimit".Translate(this._resourceLimit);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        ThingDef currentResource = this.CurrentResource;
        if (currentResource is null)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "CSP_BillableDrillLimit".Translate(),
            defaultDesc = "CSP_BillableDrillDesc".Translate(currentResource),
            icon = currentResource.uiIcon,
            action = delegate
            {
                Find.WindowStack.Add(new Dialog_Slider(
                    current => "CSP_CurrentLimit".Translate(current),
                    -1,
                    5000,
                    value =>
                    {
                        foreach (Thing extractor in this.parent.Map.listerBuildings.AllBuildingsColonistOfDef(this.parent.def))
                        {
                            CompAutoExtractorLimit limitComp = extractor.TryGetComp<CompAutoExtractorLimit>();
                            if (limitComp?.CurrentResource == currentResource)
                            {
                                limitComp._resourceLimit = value;
                            }
                        }
                    },
                    this._resourceLimit));
            },
            groupable = true,
            groupKey = currentResource.GetHashCode() * 0x114515
        };
    }

    public void CheckLimitAfterProduction(ThingDef producedResource)
    {
        if (this._resourceLimit == -1 || producedResource is null || !this.parent.Spawned)
        {
            return;
        }

        Map map = this.parent.Map;
        int resourceCount = map.listerThings.ThingsOfDef(producedResource).Sum(thing => thing.stackCount);
        if (resourceCount < this._resourceLimit)
        {
            return;
        }

        CompFlickable flickable = this.parent.TryGetComp<CompFlickable>();
        if (flickable is null)
        {
            return;
        }

        Traverse.Create(flickable).Field("wantSwitchOn").SetValue(false);
        bool wasOn = flickable.SwitchIsOn;
        flickable.SwitchIsOn = false;
        map.designationManager.DesignationOn(this.parent, DesignationDefOf.Flick)?.Delete();

        if (wasOn)
        {
            Messages.Message(
                "CSP_AutoExtractorLimitReached".Translate(this.parent.LabelCap, producedResource.LabelCap, resourceCount, this._resourceLimit),
                this.parent,
                MessageTypeDefOf.TaskCompletion);
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref this._resourceLimit, "autoExtractorResourceLimit", -1);
    }
}

[HarmonyPatch(typeof(SK.Building_Extractor), "TryProducePortion")]
public static class ExtractorResourceLimitPatch
{
    [HarmonyPrefix]
    public static void CaptureResource(SK.Building_Extractor __instance, out ThingDef __state)
    {
        __state = __instance.TryGetComp<CompAutoExtractorLimit>()?.CurrentResource;
    }

    [HarmonyPostfix]
    public static void CheckLimit(SK.Building_Extractor __instance, ThingDef __state)
    {
        __instance.TryGetComp<CompAutoExtractorLimit>()?.CheckLimitAfterProduction(__state);
    }
}

[HarmonyPatch(typeof(SK.Building_AdvancedExtractor), "TryProducePortion")]
public static class AdvancedExtractorResourceLimitPatch
{
    [HarmonyPrefix]
    public static void CaptureResource(SK.Building_AdvancedExtractor __instance, out ThingDef __state)
    {
        __state = __instance.TryGetComp<CompAutoExtractorLimit>()?.CurrentResource;
    }

    [HarmonyPostfix]
    public static void CheckLimit(SK.Building_AdvancedExtractor __instance, ThingDef __state)
    {
        __instance.TryGetComp<CompAutoExtractorLimit>()?.CheckLimitAfterProduction(__state);
    }
}
