using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AnotherTweaks
{
    public class PawnColumnWorker_AllowedArea : PawnColumnWorker
    {
        protected override GameFont DefaultHeaderFont => 0;

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), 200);
        }

        public override int GetMinHeaderHeight(PawnTable table)
        {
            return Mathf.Max(65, base.GetMinHeaderHeight(table));
        }

        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(273, GetMinWidth(table), GetMaxWidth(table));
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            if (Event.current.shift)
            {
                DoMassAreaSelector(rect, table);
                return;
            }

            if (Widgets.ButtonText(new Rect(rect.x, rect.y + (rect.height - 65f), Mathf.Min(rect.width, 360f), 32f),
                "ManageAreas".Translate())) Find.WindowStack.Add(new Dialog_ManageAreas(Find.CurrentMap));
            base.DoHeader(rect, table);
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
        }

        private int GetValueToCompare(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer) return int.MinValue;
            var areaRestriction = pawn.playerSettings.AreaRestriction;
            if (areaRestriction == null) return int.MinValue;
            return areaRestriction.ID;
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.Faction != Faction.OfPlayer) return;
            AreaAllowedGUI.DoAllowedAreaSelectors(rect, pawn);
        }

        protected override string GetHeaderTip(PawnTable table)
        {
            return base.GetHeaderTip(table) + "\n" + "AT_AllowedAreaTip".Translate();
        }

        protected override void HeaderClicked(Rect headerRect, PawnTable table)
        {
            if (Event.current.control)
            {
                Find.WindowStack.Add(new Dialog_ManageAreas(Find.CurrentMap));
                return;
            }

            base.HeaderClicked(headerRect, table);
        }

        public void DoMassAreaSelector(Rect rect, PawnTable table)
        {
            var areas = (from a in Find.CurrentMap.areaManager.AllAreas 
                where a.AssignableAsAllowed()
                select a).ToList();
            var num = areas.Count() + 1;
            var num2 = rect.width / num;
            var rect2 = new Rect(rect.x, rect.y + (rect.height - 32f), num2, /*rect.height*/32f);
            Text.WordWrap = false;
            Text.Font = 0;
            if (DoAreaSelector(rect2, null)) RestrictAllTo(null, table);
            rect2.x += num2;
            foreach (var area in areas)
            {
                if (DoAreaSelector(rect2, area)) RestrictAllTo(area, table);
                rect2.x += num2;
            }

            Text.WordWrap = true;
            Text.Font = GameFont.Small;
        }

        private void RestrictAllTo(Area area, PawnTable table)
        {
            foreach (var pawn in table.PawnsListForReading) pawn.playerSettings.AreaRestriction = area;
        }

        private bool DoAreaSelector(Rect rect, Area area)
        {
            rect = rect.ContractedBy(1f);
            GUI.DrawTexture(rect, area == null ? BaseContent.GreyTex : area.ColorTexture);
            Text.Anchor = TextAnchor.MiddleLeft;
            var rect2 = rect.ContractedBy(3f);
            var text = AreaUtility.AreaAllowedLabel_Area(area);
            Widgets.Label(rect2, text);
            TooltipHandler.TipRegion(rect, text);
            if (Mouse.IsOver(rect))
            {
                area?.MarkForDraw();
                if (Input.GetMouseButton(0))
                {
                    SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
                    return true;
                }
            }

            return false;
        }
    }
}