using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_AddFaction : Window
    {
        private readonly FactionDef factionDef;
        private readonly Action<FactionDef, int, int> spawnCallback;

        private int settlementsToSpawn;
        private int settlementsRecommended;
        private int distanceToSpawn;
        private int distanceRecommended;

        public Window_AddFaction(FactionDef factionDef, Action<FactionDef, int, int> spawnCallback)
        {
            this.factionDef = factionDef;
            this.spawnCallback = spawnCallback;
            forcePause = true;
            doCloseX = false;
            absorbInputAroundWindow = true;

            settlementsRecommended = GetRecommendedSettlementCount();
            settlementsToSpawn = Mathf.Min(4 * settlementsRecommended, GetRecommendedSettlementCount(1f));
            distanceRecommended = GetRecommendedMinDistance();
            distanceToSpawn = distanceRecommended;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(400f, 270f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            listing.Label("WB_AddFactionFactionLabel".Translate(factionDef.LabelCap));
            listing.Gap(12f);

            listing.Label("WB_AddFactionSettlementsToSpawn".Translate(settlementsRecommended, settlementsToSpawn));
            settlementsToSpawn = Mathf.CeilToInt(listing.Slider(settlementsToSpawn, 1, Mathf.Max(settlementsRecommended * 4, 10)));

            listing.Gap(12f);
            listing.Label("WB_AddFactionMinDistance".Translate(distanceRecommended, distanceToSpawn));
            distanceToSpawn = Mathf.CeilToInt(listing.Slider(distanceToSpawn, 1, distanceRecommended * 2));

            listing.Gap(24f);

            if (listing.ButtonText("WB_AddFactionSpawn".Translate()))
            {
                Spawn();
            }

            if (listing.ButtonText("Cancel".Translate()))
            {
                Close();
            }

            listing.End();
        }

        private void Spawn()
        {
            try
            {
                var faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(factionDef));
                Find.FactionManager.Add(faction);

                for (int i = 0; i < settlementsToSpawn; i++)
                {
                    var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    settlement.SetFaction(faction);
                    settlement.Tile = FindValidTileForSettlement(faction, distanceToSpawn);
                    if (settlement.Tile.Valid)
                    {
                        settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
                        Find.WorldObjects.Add(settlement);
                    }
                }

                Messages.Message("WB_AddFactionSuccess".Translate(faction.GetCallLabel(), settlementsToSpawn), MessageTypeDefOf.PositiveEvent);
                Close();
            }
            catch (Exception e)
            {
                Log.Error($"Worldbuilder: Error spawning faction {factionDef?.defName}: {e.Message}\n{e.StackTrace}");
                Messages.Message("WB_AddFactionFailed".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        private int GetRecommendedSettlementCount(float factor = 1f)
        {
            int worldTiles = Find.WorldGrid.TilesCount;
            int existingFactions = Find.FactionManager.AllFactionsListForReading.Count;
            if (existingFactions == 0)
            {
                existingFactions = 1;
            }

            float recommended = (float)Math.Sqrt(worldTiles) / (existingFactions * 2) * factor;
            return Mathf.Max(1, Mathf.CeilToInt(recommended));
        }

        private int GetRecommendedMinDistance()
        {
            return 12;
        }

        private PlanetTile FindValidTileForSettlement(Faction faction, int minDistance)
        {
            for (int i = 0; i < 100; i++)
            {
                var tile = TileFinder.RandomSettlementTileFor(faction);
                if (tile.Valid && IsValidDistanceFromPlayer(tile, minDistance))
                {
                    return tile;
                }
            }
            return PlanetTile.Invalid;
        }

        private bool IsValidDistanceFromPlayer(PlanetTile tile, int minDistance)
        {
            if (minDistance <= 0)
            {
                return true;
            }

            foreach (var settlement in Find.WorldObjects.Settlements)
            {
                if (settlement.Faction == Faction.OfPlayer && Find.WorldGrid.ApproxDistanceInTiles(tile, settlement.Tile) < minDistance)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
