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
                    var newPage = new Page_SelectWorld(createWorldParamsPage);
                    window = newPage;
                    newPage.prev = createWorldParamsPage.prev;
                    newPage.next = createWorldParamsPage.next;
                }
            }
        }
    }
}