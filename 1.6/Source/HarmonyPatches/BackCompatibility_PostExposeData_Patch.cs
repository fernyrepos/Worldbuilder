using Verse;
using RimWorld.Planet;
using HarmonyLib;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(BackCompatibility), "PostExposeData")]
    public static class BackCompatibility_PostExposeData_Patch
    {
        public static bool shouldPrevent = false;
        public static bool Prefix(object obj)
        {
            if (obj is WorldInfo info && (shouldPrevent || WorldPresetManager.shouldPrevent))
            {
                return false;
            }
            return true;
        }
    }
}
