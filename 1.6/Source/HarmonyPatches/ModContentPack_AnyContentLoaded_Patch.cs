using HarmonyLib;
using Verse;
using System.IO;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(ModContentPack), nameof(ModContentPack.AnyContentLoaded))]
    public static class ModContentPack_AnyContentLoaded_Patch
    {
        public static void Postfix(ModContentPack __instance, ref bool __result)
        {
            if (__result)
            {
                return;
            }
            foreach (var folder in __instance.foldersToLoadDescendingOrder)
            {
                string worldbuilderFolderPath = Path.Combine(folder, "Worldbuilder");
                if (Directory.Exists(worldbuilderFolderPath))
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}
