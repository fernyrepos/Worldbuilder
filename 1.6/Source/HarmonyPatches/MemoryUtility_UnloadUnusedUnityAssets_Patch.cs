using System.Linq;
using HarmonyLib;
using Verse;
using Verse.Profile;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
    public static class Game_UnloadUnusedUnityAssets_Patch
    {
        public static void Prefix()
        {
            World_ExposeData_Patch.CleanWorldData();
        }
    }
    
    [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
    public static class MemoryUtility_UnloadUnusedUnityAssets_Patch2
    {
        public static void Prefix()
        {
            World_ExposeData_Patch.CleanWorldData();
        }
    }
}
