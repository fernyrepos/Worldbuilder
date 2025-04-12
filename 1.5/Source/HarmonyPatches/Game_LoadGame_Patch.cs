using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Game), "LoadGame")]
    public static class Game_LoadGame_Patch
    {
        public static void Prefix()
        {
            MarkerDataManager.ClearData();
        }
    }
}