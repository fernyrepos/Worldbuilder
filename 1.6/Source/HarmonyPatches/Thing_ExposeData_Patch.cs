using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Thing), "ExposeData")]
    public static class Thing_ExposeData_Patch
    {
        public static void Postfix(Thing __instance)
        {
            var thingCustomation = CustomizationDataCollections.thingCustomizationData.TryGetValue(__instance, out var data) ? data : null;
            Scribe_Deep.Look(ref thingCustomation, "customizationData");
            if (thingCustomation != null)
            {
                CustomizationDataCollections.thingCustomizationData[__instance] = thingCustomation;
                if (Scribe.mode == LoadSaveMode.PostLoadInit && __instance is Pawn pawn && thingCustomation.nameOverride != null && thingCustomation.nameOverride.IsValid)
                {
                    pawn.Name = thingCustomation.nameOverride;
                }
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit && __instance is Pawn pawn)
            {
                var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
                if (currentPreset != null && currentPreset.customizationDefaults != null &&
                    currentPreset.customizationDefaults.TryGetValue(pawn.def, out var playerData))
                {
                    var dataToApply = new CustomizationData();
                    dataToApply.selectedImagePath = playerData.selectedImagePath;
                    dataToApply.narrativeText = playerData.narrativeText;
                    CustomizationDataCollections.thingCustomizationData[pawn] = dataToApply;
                }
            }
        }
    }
}
