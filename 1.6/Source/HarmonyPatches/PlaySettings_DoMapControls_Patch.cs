using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoMapControls))]
    public static class PlaySettings_DoMapControls_Patch
    {
        public static void Postfix(WidgetRow row)
        {
            row.ToggleableIcon(ref World_ExposeData_Patch.showCustomization, GizmoUtility.CustomizationToggle, "WB_CustomizeToggle".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        }
    }
}
