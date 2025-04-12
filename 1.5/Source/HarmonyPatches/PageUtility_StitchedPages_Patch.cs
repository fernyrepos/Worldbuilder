using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(PageUtility), "StitchedPages")]
    public static class PageUtility_StitchedPages_Patch
    {
        public static void Prefix(ref IEnumerable<Page> pages)
        {
            WorldPresetManager.CurrentlyLoadedPreset = null;
            var pageList = pages.ToList();

            int storytellerPageIndex = pageList.FindIndex(p => p is Page_SelectStoryteller);
            if (storytellerPageIndex != -1)
            {
                if (WorldPresetManager.GetAllPresets().Any())
                {
                    pageList.Insert(storytellerPageIndex + 1, new Page_SelectWorld());
                }
            }

            pages = pageList;
        }
    }
}