using HarmonyLib;
using RimWorld;

namespace Worldbuilder;

[HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.CanDoNext))]
public static class Page_CreateWorldParams_CanDoNext_Patch
{
    public static void Prefix()
    {
        if (Page_CreateWorldParams_DoWindowContents_Patch.thread != null)
        {
            Page_CreateWorldParams_DoWindowContents_Patch.thread.Abort();
            Page_CreateWorldParams_DoWindowContents_Patch.thread.Join(1000);
            Page_CreateWorldParams_DoWindowContents_Patch.thread = null;
        }

        Page_CreateWorldParams_DoWindowContents_Patch.generatingWorld = false;
    }
}
