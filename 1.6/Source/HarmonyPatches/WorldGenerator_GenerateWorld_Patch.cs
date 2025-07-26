using HarmonyLib;
using RimWorld.Planet;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GenerateWorld))]
    public static class WorldGenerator_GenerateWorld_Patch
    {
        public static void Prefix()
        {
            var preset = WorldPresetManager.CurrentlyLoadedPreset;
            if (preset == null) return;
            if (preset.scenParts.NullOrEmpty() is false)
            {
                Current.Game.Scenario = Current.Game.Scenario.CopyForEditing();
                foreach (var scenPart in preset.scenParts)
                {
                    var newPart = scenPart.CopyForEditing();
                    Current.Game.Scenario.parts.Add(newPart);
                    newPart.PreConfigure();
                }
            }
            ModCompatibilityHelper.TrySetMLPSubcount(preset.myLittlePlanetSubcount);
            if (preset.saveWorldTechLevel && preset.worldTechLevel != TechLevel.Undefined)
            {
                ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
            }
        }
    }
}
