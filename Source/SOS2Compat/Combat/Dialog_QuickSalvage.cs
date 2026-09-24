using SaveOurShip2;
using RimWorld.Planet;

namespace Core_SK_Patch;

internal sealed class Dialog_QuickSalvage : Window
{
    private readonly CompShipBaySalvage bay;
    private readonly MapComponent_QuickSalvage session;
    private readonly List<TransferableOneWay> rows = new List<TransferableOneWay>();
    private TransferableOneWayWidget pawnWidget, itemWidget;
    private bool pawnTab = true;

    public override Vector2 InitialSize => new Vector2(1000f, Mathf.Min(800f, UI.screenHeight));
    private float Mass => CollectionsMassCalculator.MassUsageTransferables(rows,
        IgnorePawnsInventoryMode.DontIgnore, includePawnsMass: true);

    internal Dialog_QuickSalvage(CompShipBaySalvage bay, MapComponent_QuickSalvage session)
    {
        this.bay = bay;
        this.session = session;
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = true;
        RebuildRows();
    }

    private void RebuildRows()
    {
        rows.Clear();
        foreach (Thing thing in session.Cargo.Where(t => !t.Destroyed).ToList())
        {
            if (thing is Corpse corpse && corpse.InnerPawn == null) continue;
            var row = TransferableUtility.TransferableMatching(thing, rows, TransferAsOneMode.PodsOrCaravanPacking);
            if (row == null) { row = new TransferableOneWay(); rows.Add(row); }
            row.things.Add(thing);
        }
        pawnWidget = MakeWidget(null);
        CaravanUIUtility.AddPawnsSections(pawnWidget, rows);
        itemWidget = MakeWidget(rows.Where(r => r.ThingDef.category != ThingCategory.Pawn));
    }

    private TransferableOneWayWidget MakeWidget(IEnumerable<TransferableOneWay> contents) =>
        new TransferableOneWayWidget(contents, null, null, "FormCaravanColonyThingCountTip".Translate(),
            drawMass: true, ignorePawnInventoryMass: IgnorePawnsInventoryMode.DontIgnore,
            includePawnsMassInMassUsage: true, availableMassGetter: () => QuickSalvage.Capacity(bay) - Mass,
            ignoreSpawnedCorpseGearAndInventoryMass: false, tile: session.map.Tile,
            drawMarketValue: true, drawEquippedWeapon: true, drawDaysUntilRot: true);

    public override void DoWindowContents(Rect rect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0, 0, rect.width, 35), "CoreSK_QuickSalvage".Translate());
        Text.Font = GameFont.Small;
        float capacity = QuickSalvage.Capacity(bay);
        GUI.color = Mass > capacity ? Color.red : Color.white;
        Widgets.Label(new Rect(0, 38, rect.width, 30), "CoreSK_QuickSalvageMass".Translate(Mass.ToString("F1"), capacity.ToString("F0")));
        GUI.color = Color.white;
        Widgets.Label(new Rect(0, 70, rect.width, 55), "CoreSK_QuickSalvageSelectionInfo".Translate());
        var content = new Rect(0, 162, rect.width, rect.height - 220);
        TabDrawer.DrawTabs(content, new List<TabRecord>
        {
            new TabRecord("PawnsTab".Translate(), () => pawnTab = true, pawnTab),
            new TabRecord("ItemsTab".Translate(), () => pawnTab = false, !pawnTab)
        });
        (pawnTab ? pawnWidget : itemWidget).OnGUI(content, out _);
        float y = rect.height - 45;
        if (Widgets.ButtonText(new Rect(0, y, 160, 40), "SelectEverything".Translate()))
            foreach (var row in rows) row.AdjustTo(row.GetMaximumToTransfer());
        if (Widgets.ButtonText(new Rect(170, y, 160, 40), "ResetButton".Translate()))
            foreach (var row in rows) row.AdjustTo(0);
        if (Widgets.ButtonText(new Rect(rect.width - 170, y, 160, 40), "AcceptButton".Translate()))
        {
            if (!QuickSalvage.Eligible(bay, session.map) || Mass > capacity)
            {
                Messages.Message("CoreSK_QuickSalvageOverweight".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            void Accept()
            {
                if (QuickSalvage.Deliver(bay, session, rows)) Close();
                else RebuildRows();
            }
            if (rows.Any(r => r.CountToTransfer < r.GetMaximumToTransfer()))
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CoreSK_QuickSalvageAbandon".Translate(), Accept));
            else Accept();
        }
    }
}
