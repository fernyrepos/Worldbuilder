using RimWorld;
using Verse;

namespace Worldbuilder
{
    public class ScenPart_SpawnLandmarkOnStartingTile : ScenPart
    {
        public LandmarkDef landmarkDef;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref landmarkDef, "landmarkDef");
        }

        public override void PostWorldGenerate()
        {
            if (!ModsConfig.OdysseyActive || landmarkDef == null || !Find.GameInitData.startingTile.Valid)
            {
                return;
            }

            var tile = Find.GameInitData.startingTile;
            if (Find.World.landmarks[tile] == null && landmarkDef.IsValidTile(tile, tile.Layer))
            {
                Find.World.landmarks.AddLandmark(landmarkDef, tile);
            }
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            var rect = listing.GetScenPartRect(this, RowHeight);
            string buttonLabel = landmarkDef == null ? "WB_SelectALandmark".Translate() : landmarkDef.LabelCap;
            if (Widgets.ButtonText(rect, buttonLabel))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<LandmarkDef>.AllDefs, (LandmarkDef d) => d.LabelCap, (LandmarkDef d) => delegate
                {
                    landmarkDef = d;
                });
            }
        }

        public override void Randomize()
        {
            if (DefDatabase<LandmarkDef>.AllDefsListForReading.Any())
            {
                landmarkDef = DefDatabase<LandmarkDef>.AllDefsListForReading.RandomElement();
            }
        }

        public override string Summary(Scenario scen)
        {
            return "WB_ScenPart_SpawnLandmarkOnStartingTile_Summary".Translate(landmarkDef?.LabelCap ?? "None");
        }

        public override bool HasNullDefs()
        {
            if (!base.HasNullDefs())
            {
                return landmarkDef == null;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ ((landmarkDef != null) ? landmarkDef.GetHashCode() : 0);
        }
    }
}
