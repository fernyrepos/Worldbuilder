using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;

namespace Worldbuilder
{
    public static class ResearchUtility_InitialResearchLevelFor_Patch
    {
        public static bool Prepare()
        {
            return ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId);
        }

        public static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("WorldTechLevel.ResearchUtility");
            if (type == null) return null;
            return AccessTools.Method(type, "InitialResearchLevelFor");
        }

        public static WorldPreset preset;
        public static void Postfix(ref TechLevel __result)
        {
            if (preset != null && preset.saveWorldTechLevel && preset.worldTechLevel != TechLevel.Undefined)
            {
                __result = preset.worldTechLevel;
            }
        }
    }
}
