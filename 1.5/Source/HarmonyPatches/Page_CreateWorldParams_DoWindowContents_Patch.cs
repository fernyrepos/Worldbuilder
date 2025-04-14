using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.DoWindowContents))]
    public static class Page_CreateWorldParams_DoWindowContents_Patch
    {
        private static Page_CreateWorldParams trackedInstance;
        private static string initialSeedForInstance;
        private static bool warningShownForInstance;

        public static void Postfix(Page_CreateWorldParams __instance, Rect rect)
        {
            if (__instance != trackedInstance)
            {
                trackedInstance = __instance;
                warningShownForInstance = false;
                initialSeedForInstance = null;

                var preset = WorldPresetManager.CurrentlyLoadedPreset;
                if (preset != null && preset.saveTerrain && !string.IsNullOrEmpty(preset.savedSeedString))
                {
                    initialSeedForInstance = __instance.seedString;
                }
            }
            if (initialSeedForInstance != null && !warningShownForInstance)
            {
                if (__instance.seedString != initialSeedForInstance)
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "WB_CreateWorldParamsConfirmUnlockSeedMessage".Translate(),
                        () =>
                        {
                        },
                        destructive: true,
                        title: "WB_CreateWorldParamsConfirmUnlockSeedTitle".Translate()
                    ));
                    warningShownForInstance = true;
                }
            }
        }
    }
}