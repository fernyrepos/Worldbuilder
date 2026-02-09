using UnityEngine;
using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(Printer_Plane), nameof(Printer_Plane.PrintPlane))]
    public static class Printer_Plane_PrintPlane_Patch
    {
        public static void Prefix(ref Vector3 center)
        {
            var customData = Graphic_Customized.currentPrintingData;
            if (customData == null) return;

            center += new Vector3(customData.drawOffset.x, 0, customData.drawOffset.y);

            if (customData.altitudeLayer.HasValue)
            {
                center.y = Altitudes.AltitudeFor(customData.altitudeLayer.Value);
            }
        }
    }
}
