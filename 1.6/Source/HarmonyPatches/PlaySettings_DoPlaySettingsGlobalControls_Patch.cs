using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class PlaySettings_DoPlaySettingsGlobalControls_Patch
    {
        public static void Postfix(WidgetRow row, bool worldView)
        {
            row.ToggleableIcon(ref World_ExposeData_Patch.showCustomization, GizmoUtility.CustomizationToggle, "WB_CustomizeToggle".Translate(), SoundDefOf.Mouseover_ButtonToggle);
            if (!worldView &&
                Current.ProgramState == ProgramState.Playing &&
                Find.CurrentMap != null &&
                WorldRendererUtility.DrawingMap &&
                MapGenerator.mapBeingGenerated == null &&
                row.ButtonIcon(
                    GizmoUtility.TileBrushIcon,
                    "WB_TileBrushOpenLocalEditor".Translate()))
            {
                TileBrushSessionManager.OpenEditor(Find.CurrentMap);
            }
        }
    }
}
