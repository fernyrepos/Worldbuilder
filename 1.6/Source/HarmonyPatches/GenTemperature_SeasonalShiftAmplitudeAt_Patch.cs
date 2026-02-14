using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(GenTemperature), nameof(GenTemperature.SeasonalShiftAmplitudeAt))]
public static class GenTemperature_SeasonalShiftAmplitudeAt_Patch
{
    public static bool Prefix()
    {
        return false;
    }

    [HarmonyPriority(int.MaxValue)]
    public static void Postfix(PlanetTile tile, ref float __result)
    {
        if (Find.WorldGrid.LongLatOf(tile).y >= 0f)
        {
            __result = World_FinalizeInit_Patch.mappedValues[World_FinalizeInit_Patch.axialTilt]
                .Evaluate(Find.WorldGrid.DistanceFromEquatorNormalized(tile));
            return;
        }

        __result = -World_FinalizeInit_Patch.mappedValues[World_FinalizeInit_Patch.axialTilt]
            .Evaluate(Find.WorldGrid.DistanceFromEquatorNormalized(tile));
    }
}
