using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using System.Collections.Generic;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Add))]
    public static class WindowStack_Add_Patch
    {
        public static void Prefix(ref Window window)
        {
            if (window is Page_CreateWorldParams createWorldParamsPage && Find.WindowStack.IsOpen<Page_SelectWorld>() is false)
            {
                if (WorldPresetManager.GetAllPresets(true).Any())
                {
                    if (createWorldParamsPage.prev is not Page_SelectWorld)
                    {
                        var newPage = new Page_SelectWorld(createWorldParamsPage);
                        newPage.prev = createWorldParamsPage.prev;
                        newPage.next = createWorldParamsPage.next;
                        window = newPage;
                    }
                    else if (createWorldParamsPage.prev is Page_SelectWorld page_SelectWorld)
                    {
                        if (WorldPresetManager.CurrentlyLoadedPreset is null)
                        {
                            
                        }
                        else
                        {
                            window = page_SelectWorld;
                        }
                    }
                }
            }
        }
    }
}
