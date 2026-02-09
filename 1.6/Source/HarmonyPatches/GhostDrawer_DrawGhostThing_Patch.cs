using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(GhostDrawer), nameof(GhostDrawer.DrawGhostThing))]
    public static class GhostDrawer_DrawGhostThing_Patch
    {
        public static void Prefix(ThingDef thingDef, ref Graphic baseGraphic, Thing thing)
        {
            var data = thingDef.GetCustomizationDataPlayer();
            if (data != null)
            {
                var stuff = thing?.Stuff;
                var customGraphic = data.GetGraphicForDef(thingDef, stuff);
                if (customGraphic != null)
                {
                    baseGraphic = customGraphic;
                }
            }
        }
    }
}
