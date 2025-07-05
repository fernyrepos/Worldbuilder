using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{

    [HotSwappable]
    public class Window_MapTextEditor : Window
    {
        private Vector2 scrollPositionLabels = Vector2.zero;
        private WorldFeature selectedFeature = null;
        private string xPosBuffer = "";
        private string yPosBuffer = "";
        public override Vector2 InitialSize => new Vector2(600f, 500f);
        public Window_MapTextEditor()
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            selectedFeature = Find.World.features.features.FirstOrDefault();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect leftRect = new Rect(inRect.x, inRect.y, inRect.width * 0.4f - 10f, inRect.height);
            Rect rightRect = new Rect(leftRect.xMax + 10f, inRect.y, inRect.width * 0.6f, inRect.height);

            DrawLabelList(leftRect);
            DrawEditPanel(rightRect);
        }

        private void DrawLabelList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(10f));

            Text.Font = GameFont.Medium;
            listing.Label("Map text");
            Text.Font = GameFont.Small;
            listing.GapLine();

            float listScrollViewHeight = rect.height - listing.CurHeight;
            Rect scrollRectOuter = listing.GetRect(listScrollViewHeight);
            var labelFeatures = Find.World.features.features.ToList();
            float viewHeight = labelFeatures.Count * Text.LineHeight;
            Rect viewRect = new Rect(0f, 0f, scrollRectOuter.width - 16f, viewHeight);

            Widgets.BeginScrollView(scrollRectOuter, ref scrollPositionLabels, viewRect);
            float currentY = 0f;
            foreach (var feature in labelFeatures)
            {
                Rect rowRect = new Rect(0f, currentY, viewRect.width, Text.LineHeight);
                string label = feature.name ?? "[No Name]";

                if (selectedFeature == feature)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (Widgets.ButtonInvisible(rowRect))
                {
                    SelectFeature(feature);
                }
                Widgets.Label(rowRect.ContractedBy(2f), label);
                currentY += Text.LineHeight;
            }
            Widgets.EndScrollView();
            listing.Gap(10f);
            if (Widgets.ButtonText(listing.GetRect(30f), "Add map text"))
            {
                AddFeature("New Map Text", Find.WorldGrid.TilesCount / 2); // Placeholder tile for new feature
            }
            listing.End();
        }

        private void DrawEditPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(15f));

            string labelText = selectedFeature?.name ?? "";
            Rect labelTextRect = listing.GetRect(Text.LineHeight);
            labelText = Widgets.TextField(labelTextRect, labelText);

            listing.Gap(10f);

            int tileId = GetTileIdForFeature(selectedFeature);
            Vector2 coords = (tileId != -1) ? Find.WorldGrid.LongLatOf(tileId) : Vector2.zero;

            Rect xLabelRect = listing.GetRect(Text.LineHeight);
            Widgets.Label(xLabelRect.LeftHalf(), "X-Position");
            int xPos = Mathf.RoundToInt(coords.x);
            Rect xPosRect = xLabelRect.RightHalf();
            Widgets.TextFieldNumeric(xPosRect, ref xPos, ref xPosBuffer, min: int.MinValue, max: int.MaxValue);

            Rect yLabelRect = listing.GetRect(Text.LineHeight);
            Widgets.Label(yLabelRect.LeftHalf(), "Y-Position");
            int yPos = Mathf.RoundToInt(coords.y);
            Rect yPosRect = yLabelRect.RightHalf();
            Widgets.TextFieldNumeric(yPosRect, ref yPos, ref yPosBuffer, min: int.MinValue, max: int.MaxValue);
            SaveChanges(labelText, FindTile(xPos, yPos));

            listing.Gap(10f);
            if (selectedFeature != null && Widgets.ButtonText(listing.GetRect(30f), "Remove"))
            {
                RemoveFeature(selectedFeature);
            }
            listing.End();
        }

        private void SelectFeature(WorldFeature feature)
        {
            selectedFeature = feature;
        }

        private void ClearSelection()
        {
            selectedFeature = null;
        }

        private void AddFeature(string labelText, int tileId)
        {
            if (Find.WorldGrid[tileId].feature != null)
            {
                Messages.Message($"Tile {tileId} already has a feature: {Find.WorldGrid[tileId].feature.def.label}", MessageTypeDefOf.RejectInput);
                return;
            }

            WorldFeature newFeature = new WorldFeature();
            newFeature.def = DefsOf.WB_MapLabelFeature;
            newFeature.uniqueID = Find.UniqueIDsManager.GetNextWorldFeatureID();
            newFeature.name = labelText;

            Find.WorldGrid[tileId].feature = newFeature;
            Find.World.features.features.Add(newFeature);

            Find.WorldFeatures.textsCreated = false;
            Messages.Message($"Added label '{labelText}' to tile {tileId}", MessageTypeDefOf.PositiveEvent);
            ClearSelection();
        }

        private void SaveChanges(string newLabelText, int newTileId)
        {
            if (selectedFeature == null) return;

            int oldTileId = GetTileIdForFeature(selectedFeature);

            selectedFeature.name = newLabelText;

            if (oldTileId != newTileId)
            {
                if (Find.WorldGrid[newTileId].feature != null)
                {
                    Messages.Message($"Target Tile {newTileId} already has a feature: {Find.WorldGrid[newTileId].feature.def.label}", MessageTypeDefOf.RejectInput);
                    return;
                }
                if (oldTileId >= 0)
                {
                    Find.WorldGrid[oldTileId].feature = null;
                }
                Find.WorldGrid[newTileId].feature = selectedFeature;
            }

            Find.WorldFeatures.textsCreated = false;
            Messages.Message($"Updated label '{newLabelText}'", MessageTypeDefOf.PositiveEvent);
            ClearSelection();
        }

        private void RemoveFeature(WorldFeature feature)
        {
            Dialog_MessageBox confirmationDialog = Dialog_MessageBox.CreateConfirmation(
               $"Remove label '{feature.name ?? "[No Name]"}'?",
               () =>
               {
                   int tileId = GetTileIdForFeature(feature);
                   if (tileId >= 0 && Find.WorldGrid[tileId].feature == feature)
                   {
                       Find.WorldGrid[tileId].feature = null;
                   }
                   Find.World.features.features.Remove(feature);
                   Find.WorldFeatures.textsCreated = false;
                   Messages.Message($"Removed label '{feature.name ?? "[No Name]"}'", MessageTypeDefOf.PositiveEvent);
                   ClearSelection();
               }
           );
            Find.WindowStack.Add(confirmationDialog);
        }

        private int GetTileIdForFeature(WorldFeature feature)
        {
            if (feature == null) return -1;
            for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
            {
                if (Find.WorldGrid[i].feature == feature) return i;
            }
            return -1;
        }

        private int FindTile(int x, int y)
        {
            for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
            {
                Vector2 coords = Find.WorldGrid.LongLatOf(i);
                if (Mathf.RoundToInt(coords.x) == x && Mathf.RoundToInt(coords.y) == y)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
