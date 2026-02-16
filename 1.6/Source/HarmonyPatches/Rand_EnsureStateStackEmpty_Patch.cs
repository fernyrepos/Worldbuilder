using HarmonyLib;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(Rand), nameof(Rand.EnsureStateStackEmpty))]
public static class Rand_EnsureStateStackEmpty_Patch
{
    public static bool Prefix()
    {
        if (!WorldbuilderMod.settings.enablePlanetGenOverhaul) return true;

        return !Page_CreateWorldParams_DoWindowContents_Patch.generatingWorld;
    }
}
