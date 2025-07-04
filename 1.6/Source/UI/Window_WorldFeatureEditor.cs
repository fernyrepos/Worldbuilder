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


        private string currentLabelText = "";
        private string currentTileIdStr = "";
        private int? initialTileId = null;

        private static readonly FeatureDef MapLabelDef = DefDatabase<FeatureDef>.GetNamed("WB_MapLabelFeature");

        public override Vector2 InitialSize => new Vector2(600f, 500f);


        public Window_MapTextEditor(int? selectedTile = null)
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            initialTileId = selectedTile;
            if (selectedTile.HasValue)
            {
                currentTileIdStr = selectedTile.Value.ToString();
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();

            if (selectedFeature == null && initialTileId.HasValue)
            {
                var featureOnTile = Find.WorldGrid[initialTileId.Value].feature;
                if (featureOnTile != null && featureOnTile.def == MapLabelDef)
                {
                    SelectFeature(featureOnTile);
                }
            }
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
            listing.Label("Map Labels");
            Text.Font = GameFont.Small;
            listing.GapLine();

            float listScrollViewHeight = rect.height - listing.CurHeight;
            Rect scrollRectOuter = listing.GetRect(listScrollViewHeight);
            var labelFeatures = Find.World.features.features.Where(f => f.def == MapLabelDef).ToList();
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
            listing.End();
        }

        private void DrawEditPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(15f));

            if (selectedFeature != null)
            {
                listing.Label($"Editing: {selectedFeature.name ?? "[No Name]"}");
                listing.Gap(10f);
            }
            else
            {
                listing.Label("Add New Map Label");
                listing.Gap(10f);
            }

            listing.Label("Label Text:");
            currentLabelText = listing.TextEntry(currentLabelText);

            listing.Label("Tile ID:");
            string previousTileIdStr = currentTileIdStr;
            currentTileIdStr = listing.TextEntry(currentTileIdStr);


            bool tileValid = false;
            int currentTileId = -1;
            if (int.TryParse(currentTileIdStr, out currentTileId))
            {
                tileValid = Find.WorldGrid.InBounds(currentTileId);
                if (!tileValid)
                {
                    Rect labelRect = listing.GetRect(0); labelRect.y -= Text.LineHeight; Widgets.Label(labelRect, " (Invalid Tile ID)");
                }
                else
                {
                    Vector2 coords = Find.WorldGrid.LongLatOf(currentTileId);
                    Rect coordLabelRect = listing.GetRect(0); coordLabelRect.y -= Text.LineHeight; Widgets.Label(coordLabelRect, $" (Coords: {coords.x:F2}, {coords.y:F2})");
                }
            }
            else if (!string.IsNullOrEmpty(currentTileIdStr))
            {
                Rect numericLabelRect = listing.GetRect(0); numericLabelRect.y -= Text.LineHeight; Widgets.Label(numericLabelRect, " (Enter numeric Tile ID)");
            }


            listing.Gap(20f);

            if (selectedFeature != null)
            {

                if (listing.ButtonText("Save Changes") && tileValid && !string.IsNullOrWhiteSpace(currentLabelText))
                {
                    SaveChanges(currentLabelText, currentTileId);
                }
                if (listing.ButtonText("Cancel Selection"))
                {
                    ClearSelection();
                }
                if (listing.ButtonText("Remove Label"))
                {
                    RemoveFeature(selectedFeature);
                }
            }
            else
            {

                if (listing.ButtonText("Add Label") && tileValid && !string.IsNullOrWhiteSpace(currentLabelText))
                {
                    AddFeature(currentLabelText, currentTileId);
                }
            }


            listing.End();
        }

        private void SelectFeature(WorldFeature feature)
        {
            selectedFeature = feature;
            currentLabelText = feature.name ?? "";
            currentTileIdStr = GetTileIdForFeature(feature).ToString();
        }

        private void ClearSelection()
        {
            selectedFeature = null;
            currentLabelText = "";
            currentTileIdStr = initialTileId?.ToString() ?? "";
        }


        private void AddFeature(string labelText, int tileId)
        {
            if (Find.WorldGrid[tileId].feature != null)
            {
                Messages.Message($"Tile {tileId} already has a feature: {Find.WorldGrid[tileId].feature.def.label}", MessageTypeDefOf.RejectInput);
                return;
            }

            WorldFeature newFeature = new WorldFeature();
            newFeature.def = MapLabelDef;
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
                    currentTileIdStr = oldTileId.ToString();
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
    }
}
