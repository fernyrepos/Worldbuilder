using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
namespace Worldbuilder
{
    public class WorldbuilderSettings : ModSettings
    {
        public static WorldGenerationData curWorldGenerationPreset;

        public bool showPreview = true;
        public bool enablePlanetGenOverhaul = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pawnPortraitSize, "pawnPortraitSize", 240);
            Scribe_Values.Look(ref showCustomizeGizmoOnThings, "showCustomizeGizmoOnThings", true);
            Scribe_Values.Look(ref showCustomizeGizmoOnPawns, "showCustomizeGizmoOnPawns", true);
            Scribe_Values.Look(ref showCustomizeGizmoOnPlayerColony, "showCustomizeGizmoOnPlayerColony", true);
            Scribe_Values.Look(ref showCustomizeGizmoOnFactionBases, "showCustomizeGizmoOnFactionBases", true);
            Scribe_Values.Look(ref showCustomizeGizmoOnMapMarkers, "showCustomizeGizmoOnMapMarkers", true);
            Scribe_Values.Look(ref showContentSourceOnScrollWindow, "showContentSourceOnScrollWindow", false);
            Scribe_Values.Look(ref showPreview, "showPreview", true);
            Scribe_Values.Look(ref enablePlanetGenOverhaul, "enablePlanetGenOverhaul", true);
            Scribe_Collections.Look(ref recentColors, "recentColors", LookMode.Value);
            recentColors ??= new List<Color>();
            Scribe_Collections.Look(ref tileBrushFavoriteKeys, "tileBrushFavoriteKeys", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                tileBrushFavoriteKeys = (tileBrushFavoriteKeys ?? new List<string>())
                    .Where(key => !key.NullOrEmpty())
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }
        }

        public float pawnPortraitSize = 240;
        public bool showCustomizeGizmoOnThings = true;
        public bool showCustomizeGizmoOnPawns = true;
        public bool showCustomizeGizmoOnPlayerColony = true;
        public bool showCustomizeGizmoOnFactionBases = true;
        public bool showCustomizeGizmoOnMapMarkers = true;
        public bool showContentSourceOnScrollWindow;
        public List<Color> recentColors = new List<Color>();
        public List<string> tileBrushFavoriteKeys = new List<string>();
    }
}
