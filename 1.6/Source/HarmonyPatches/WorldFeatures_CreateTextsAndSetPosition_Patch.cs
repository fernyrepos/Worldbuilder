using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldFeatures), "CreateTextsAndSetPosition")]
    public static class WorldFeatures_CreateTextsAndSetPosition_Patch
    {
        public static void Prefix(WorldFeatures __instance)
        {
            foreach (var feature in __instance.features)
            {
                if (feature.layer is null)
                {
                    feature.layer = Find.WorldGrid.Surface;
                    var newTileId = Window_MapTextEditor.GetTileIdForFeature(feature);
                    feature.drawCenter = Find.WorldGrid.GetTileCenter(newTileId);
                }
            }
        }
    }
}
