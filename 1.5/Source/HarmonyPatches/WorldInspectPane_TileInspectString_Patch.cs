using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldInspectPane), "TileInspectString", MethodType.Getter)]
    public static class WorldInspectPane_TileInspectString_Patch
    {
        public static void Postfix(ref string __result)
        {
            if (Find.WorldSelector.selectedTile >= 0)
            {
                int tileID = Find.WorldSelector.selectedTile;
                Vector2 coords = Find.WorldGrid.LongLatOf(tileID);
                string coordsText = $"Tile Coordinates: X={coords.x:F2}, Y={coords.y:F2}";
                __result += "\n" + coordsText;
            }
        }
    }
}