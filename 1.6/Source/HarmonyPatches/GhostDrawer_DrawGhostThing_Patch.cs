using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(GhostDrawer), nameof(GhostDrawer.DrawGhostThing))]
    public static class GhostDrawer_DrawGhostThing_Patch
    {
        public static void Prefix(ThingDef thingDef, ref Graphic baseGraphic, Thing thing)
        {
            CustomizationData data = thingDef.GetCustomizationDataPlayer();
            if (data != null)
            {
                var stuff = thing?.Stuff;
                Graphic customGraphic = data.GetGraphicForDef(thingDef, stuff);
                if (customGraphic != null)
                {
                    baseGraphic = customGraphic;
                }
            }
        }
    }
}
