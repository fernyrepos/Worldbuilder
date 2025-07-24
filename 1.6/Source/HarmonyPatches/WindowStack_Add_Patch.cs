using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse.Profile;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Add))]
    public static class WindowStack_Add_Patch
    {
        public static bool Prefix(Window window)
        {
            if (window is Page_CreateWorldParams)
            {
                var scenPart = Current.Game.Scenario?.parts.OfType<ScenPart_StartInWorld>().FirstOrDefault();
                if (scenPart != null && !scenPart.worldPresetName.NullOrEmpty())
                {
                    var preset = WorldPresetManager.GetPreset(scenPart.worldPresetName);
                    if (preset != null)
                    {
                        LongEventHandler.QueueLongEvent(delegate
                        {
                            World_ExposeData_Patch.WorldPresetName = preset.name;
                            GenerateWorldAndLoad(preset, window);
                        }, "GeneratingWorld", doAsynchronously: true, null);

                        return false;
                    }
                }
            }

            if (window is Page_CreateWorldParams createWorldParamsPage && !(Find.WindowStack.IsOpen<Page_SelectWorld>()))
            {
                if (WorldPresetManager.GetAllPresets(true).Any())
                {
                    if (!(createWorldParamsPage.prev is Page_SelectWorld))
                    {
                        var newPage = new Page_SelectWorld(createWorldParamsPage)
                        {
                            prev = createWorldParamsPage.prev,
                            next = createWorldParamsPage.next
                        };
                        Find.WindowStack.Add(newPage);
                        return false;
                    }
                    else if (createWorldParamsPage.prev is Page_SelectWorld page_SelectWorld)
                    {
                        if (WorldPresetManager.CurrentlyLoadedPreset is null)
                        {
                            createWorldParamsPage.seedString = GenText.RandomSeedString();
                        }
                        else
                        {
                            Find.WindowStack.Add(page_SelectWorld);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private static void GenerateWorldAndLoad(WorldPreset preset, Window originalWindow)
        {
            WorldGeneratorUtility.GenerateWorldFromPreset(preset, delegate
            {
                if (originalWindow is Page_CreateWorldParams createWorldParamsPage)
                {
                    var nextPage = createWorldParamsPage.next;
                    nextPage.prev = createWorldParamsPage.prev;
                    Find.WindowStack.Add(nextPage);
                }
            });
        }
    }
}
