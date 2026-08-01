using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    internal static class RockOverrideService
    {
        internal static WorldComponent_RockOverrides Get(RimWorld.Planet.World world = null)
        {
            return (world ?? Find.World)?.GetComponent<WorldComponent_RockOverrides>();
        }

        internal static bool TryGetOverride(
            RimWorld.Planet.World world,
            PlanetTile tile,
            out List<ThingDef> rocks)
        {
            rocks = null;
            return world != null &&
                   IsEditableSurfaceTile(world, tile) &&
                   Get(world)?.TryGetOverride(tile, out rocks) == true;
        }

        internal static bool SetOverride(PlanetTile tile, IEnumerable<ThingDef> rocks)
        {
            return Get()?.SetOverride(tile, rocks) == true;
        }

        internal static void ClearOverride(PlanetTile tile)
        {
            Get()?.ClearOverride(tile);
        }

        internal static bool HasOverride(PlanetTile tile)
        {
            return Get()?.HasOverride(tile) == true;
        }

        internal static bool IsEditableSurfaceTile(RimWorld.Planet.World world, PlanetTile tile)
        {
            return world != null &&
                   tile.Valid &&
                   tile.Layer == world.grid.Surface &&
                   tile.tileId >= 0 &&
                   tile.tileId < world.grid.TilesCount;
        }
    }
}
