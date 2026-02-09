using HarmonyLib;
using Verse.Profile;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    public static class MemoryUtility_ClearAllMapsAndWorld_Patch
    {
        public static void Prefix()
        {
            World_ExposeData_Patch.CleanWorldData();
        }
    }
}
