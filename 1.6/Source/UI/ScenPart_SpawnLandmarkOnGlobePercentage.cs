using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class ScenPart_SpawnLandmarkOnGlobePercentage : ScenPart
    {
        public LandmarkDef landmarkDef;
        public float percentage;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref landmarkDef, "landmarkDef");
            Scribe_Values.Look(ref percentage, "percentage", 1f);
        }

        public override void PostWorldGenerate()
        {
            if (!ModsConfig.OdysseyActive || landmarkDef == null)
            {
                return;
            }

            var layer = Find.WorldGrid.Surface;
            var validTiles = new List<PlanetTile>();

            foreach (var tile in layer.Tiles)
            {
                if (!tile.WaterCovered)
                {
                    if (Find.World.landmarks[tile.tile] == null)// && landmarkDef.IsValidTile(tile.tile, layer))
                    {
                        validTiles.Add(tile.tile);
                    }
                }
            }

            int tilesToSpawn = Mathf.FloorToInt(validTiles.Count * percentage);

            for (int i = 0; i < tilesToSpawn && validTiles.Count > 0; i++)
            {
                int index = Rand.RangeInclusive(0, validTiles.Count - 1);
                var tile = validTiles[index];
                Find.World.landmarks.AddLandmark(landmarkDef, tile);
                validTiles.RemoveAt(index);
            }
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            var rect = listing.GetScenPartRect(this, RowHeight + 10);
            string buttonLabel = landmarkDef == null ? "WB_SelectALandmark".Translate() : landmarkDef.LabelCap;
            rect.width -= 100;
            if (Widgets.ButtonText(rect, buttonLabel))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<LandmarkDef>.AllDefs, (LandmarkDef d) => d.LabelCap, (LandmarkDef d) => delegate
                {
                    landmarkDef = d;
                });
            }
            var sliderRect = new Rect(rect.xMax, rect.y, 100, rect.height);
            percentage = Widgets.HorizontalSlider(sliderRect, percentage, 0.01f, 1f, middleAlignment: true, percentage.ToStringPercent(), null, null, 0.01f);
        }

        public override void Randomize()
        {
            if (DefDatabase<LandmarkDef>.AllDefsListForReading.Any())
            {
                landmarkDef = DefDatabase<LandmarkDef>.AllDefsListForReading.RandomElement();
                percentage = Rand.Range(0f, 1f);
            }
        }

        public override string Summary(Scenario scen)
        {
            return "WB_ScenPart_SpawnLandmarkOnGlobePercentage_Summary".Translate(landmarkDef?.LabelCap ?? "None", percentage.ToStringPercent());
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
            return base.GetHashCode() ^ ((landmarkDef != null) ? landmarkDef.GetHashCode() : 0) ^ percentage.GetHashCode();
        }
    }
}
