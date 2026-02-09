using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    [StaticConstructorOnStartup]
    public static class WorldbuilderModLatePatching
    {
        static WorldbuilderModLatePatching()
        {
            var harmony = new Harmony("WorldbuilderModLatePatching");
            if (WorldTechLevel_WITab_Planet_FillTab_Postfix_RectTranspiler.Prepare())
            {
                var method = WorldTechLevel_WITab_Planet_FillTab_Postfix_RectTranspiler.TargetMethod();
                harmony.Patch(method, transpiler: new HarmonyMethod(typeof(WorldTechLevel_WITab_Planet_FillTab_Postfix_RectTranspiler), nameof(WorldTechLevel_WITab_Planet_FillTab_Postfix_RectTranspiler.Transpiler)));
            }
            if (ResearchUtility_InitialResearchLevelFor_Patch.Prepare())
            {
                var method = ResearchUtility_InitialResearchLevelFor_Patch.TargetMethod();
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(ResearchUtility_InitialResearchLevelFor_Patch), nameof(ResearchUtility_InitialResearchLevelFor_Patch.Postfix)));
            }
        }
    }
}
