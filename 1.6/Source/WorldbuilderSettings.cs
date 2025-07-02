using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
namespace Worldbuilder
{
    public class WorldbuilderSettings : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pawnPortraitSize, "pawnPortraitSize", 136f);
            Scribe_Values.Look(ref showCustomizeGizmoOnThings, "showCustomizeGizmoOnThings", true);
            Scribe_Values.Look(ref showCustomizeGizmoOnPawns, "showCustomizeGizmoOnPawns", true);
            Scribe_Values.Look(ref showCustomizeGizmoOnPlayerColony, "showCustomizeGizmoOnPlayerColony", true);
            Scribe_Values.Look(ref showCustomizeGizmoOnFactionBases, "showCustomizeGizmoOnFactionBases", true);
            Scribe_Values.Look(ref showCustomizeGizmoOnMapMarkers, "showCustomizeGizmoOnMapMarkers", true);
        }

        public float pawnPortraitSize = 136f;
        public bool showCustomizeGizmoOnThings = true;
        public bool showCustomizeGizmoOnPawns = true;
        public bool showCustomizeGizmoOnPlayerColony = true;
        public bool showCustomizeGizmoOnFactionBases = true;
        public bool showCustomizeGizmoOnMapMarkers = true;
    }
}