using System.Collections.Generic;

namespace Worldbuilder
{
    public static class StoryUtility
    {
        public static List<Story> GetStories()
        {
            var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
            if (currentPreset != null && currentPreset.saveStorykeeperEntries)
            {
                return currentPreset.presetStories;
            }
            else
            {
                return Game_ExposeData_Patch.worldStories;
            }
        }
    }
}