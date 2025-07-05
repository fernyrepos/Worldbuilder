using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class PlaySettings_DoPlaySettingsGlobalControls_Patch
    {
        public static void Postfix(WidgetRow row)
        {
            row.ToggleableIcon(ref World_ExposeData_Patch.showCustomization, GizmoUtility.CustomizationToggle, "WB_CustomizeToggle".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        }
    }
}
