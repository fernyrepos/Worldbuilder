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
                __result += "\n" + "WB_TileCoordinates".Translate(coords.x, coords.y);
            }
        }
    }
}
