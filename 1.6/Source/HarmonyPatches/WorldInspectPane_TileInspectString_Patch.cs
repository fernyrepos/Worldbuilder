using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldInspectPane), "TileInspectString", MethodType.Getter)]
    public static class WorldInspectPane_TileInspectString_Patch
    {
        public static void Postfix(ref string __result)
        {
            if (Find.WorldSelector.selectedTile.Valid)
            {
                PlanetTile tileID = Find.WorldSelector.selectedTile;
                var coords = Find.WorldGrid.LongLatOf(tileID);
                __result += "\n" + "WB_TileCoordinates".Translate(coords.x, coords.y);
            }
        }
    }
}
