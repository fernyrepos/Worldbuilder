using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    public static class SettlementActionUtility
    {
        public static void BeginRelocate(Settlement settlement)
        {
            Find.WorldTargeter.BeginTargeting(
                (GlobalTargetInfo target) =>
                {
                    settlement.Tile = target.Tile;
                    Messages.Message("WB_SettlementRelocated".Translate(settlement.Label), MessageTypeDefOf.NeutralEvent);
                    settlement.drawPosCacheTick = -1;
                    if (ModsConfig.IsActive(ModCompatibilityHelper.FactionTerritoriesPackageId))
                    {
                        ModCompatibilityHelper.RequestFactionTerritoryRegenerate();
                    }
                    return true;
                }, true, null, false, null,
                (GlobalTargetInfo target) =>
                {
                    return CanRelocateTo(target);
                }, canSelectTarget: (GlobalTargetInfo target) => CanRelocateTo(target) == null
            );
        }

        public static TaggedString CanRelocateTo(GlobalTargetInfo target)
        {
            if (Find.World.Impassable(target.Tile) || target.Tile.Tile.biome.impassable || target.Tile.Tile.hilliness == Hilliness.Impassable)
            {
                return "Impassable".Translate();
            }
            if (Find.WorldObjects.AnyMapParentAt(target.Tile))
            {
                return "WB_TileOccupied".Translate();
            }
            return null;
        }
    }
}
