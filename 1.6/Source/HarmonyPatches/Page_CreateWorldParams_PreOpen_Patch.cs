using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.PreOpen))]
public static class Page_CreateWorldParams_PreOpen_Patch
{
    private static void Prefix()
    {
        Page_CreateWorldParams_DoWindowContents_Patch.startFresh = true;
        if (World_ExposeData_Patch.worldGenerationData == null)
        {
            World_ExposeData_Patch.worldGenerationData = new WorldGenerationData();
            World_ExposeData_Patch.worldGenerationData.Init();
        }
    }
}
