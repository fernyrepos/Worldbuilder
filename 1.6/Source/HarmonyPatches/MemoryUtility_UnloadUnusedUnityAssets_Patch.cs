using HarmonyLib;
using Verse;
using Verse.Profile;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.UnloadUnusedUnityAssets))]
    public static class MemoryUtility_UnloadUnusedUnityAssets_Patch
    {
        public static void Postfix()
        {
            World_ExposeData_Patch.CleanWorldData();
        }
    }
}
