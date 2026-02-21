using HarmonyLib;
using RimWorld;
using Verse.Profile;

namespace Worldbuilder;

[HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.CanDoNext))]
public static class Page_CreateWorldParams_CanDoNext_Patch
{
    public static void Prefix()
    {
        if (!WorldbuilderMod.settings.enablePlanetGenOverhaul) return;

        var previewThread = Page_CreateWorldParams_DoWindowContents_Patch.thread;
        if (previewThread != null && previewThread.IsAlive)
        {
            previewThread.Abort();
        }

        Page_CreateWorldParams_DoWindowContents_Patch.thread = null;
        Page_CreateWorldParams_DoWindowContents_Patch.threadedWorld = null;
        Page_CreateWorldParams_DoWindowContents_Patch.generatingWorld = false;
    }
}
