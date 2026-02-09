using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Blueprint), "Graphic", MethodType.Getter)]
    public static class Blueprint_Graphic_Patch
    {
        public static void Postfix(Blueprint __instance, ref Graphic __result)
        {
            ThingDef thingDefToBuild = __instance.def.entityDefToBuild as ThingDef;
            if (thingDefToBuild == null)
            {
                return;
            }

            var data = thingDefToBuild.GetCustomizationDataPlayer();
            if (data != null)
            {
                var customGraphic = data.GetGraphicForDef(thingDefToBuild, __instance.EntityToBuildStuff());
                if (customGraphic != null)
                {
                    __result = customGraphic.GetColoredVersion(ShaderTypeDefOf.EdgeDetect.Shader, ThingDefGenerator_Buildings.BlueprintColor, Color.white);
                }
            }
        }
    }
}
