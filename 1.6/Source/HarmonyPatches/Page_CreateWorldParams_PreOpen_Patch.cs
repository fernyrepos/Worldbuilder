using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.PreOpen))]
public static class Page_CreateWorldParams_PreOpen_Patch
{
    private static void Prefix()
    {
        Log.Message("[RG] PreOpen: Page is opening, initializing preview generation");
        Page_CreateWorldParams_DoWindowContents_Patch.startFresh = true;
    }
}
