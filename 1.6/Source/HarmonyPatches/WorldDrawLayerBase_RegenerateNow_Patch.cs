using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(WorldDrawLayerBase), nameof(WorldDrawLayerBase.RegenerateNow))]
public static class WorldDrawLayerBase_RegenerateNow_Patch
{
    public static bool Prefix()
    {
        return !Page_CreateWorldParams_DoWindowContents_Patch.dirty ||
               Find.WindowStack.WindowOfType<Page_CreateWorldParams>() == null;
    }
}
