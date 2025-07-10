using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Worldbuilder;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(WITab_Planet), nameof(WITab_Planet.FillTab))]
    public static class WITab_Planet_FillTab_Patch
    {
        public static void Postfix(WITab_Planet __instance, ref Vector2 ___WinSize)
        {
            ___WinSize = __instance.size = new Vector2(400f, 300f);
            __instance.labelKey = "WB_World";
            Rect rect = new Rect(5f, __instance.size.y - 37f * 4, 150, 32);
            if (ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId))
            {
                rect.y -= 37f;
            }
            if (Widgets.ButtonText(rect, "WB_GizmoEditMapTextLabel".Translate()))
            {
                Find.WindowStack.Add(new Window_MapTextEditor());
            }
            rect.y += 37f;
            if (Widgets.ButtonText(rect, "WB_SaveAsWorldPresetLabel".Translate()))
            {
                Find.WindowStack.Add(new Window_CreateOrEditWorld(WorldPresetManager.CurrentlyLoadedPreset, enableAllCheckboxes: true));
            }
            rect.y += 37f;
            if (Widgets.ButtonText(rect, "WB_EditExistingWorldLabel".Translate()))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var preset in WorldPresetManager.GetAllPresets(true))
                {
                    WorldPreset localPreset = preset;
                    floatMenuOptions.Add(new FloatMenuOption(localPreset.name, () => Find.WindowStack.Add(new Window_CreateOrEditWorld(localPreset, enableAllCheckboxes: false, isEditingExistingPreset: true))));
                }
                Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
            }
            rect.y += 37f;
            if (Widgets.ButtonText(rect, "WB_TransitionWorldLabel".Translate()))
            {
                Find.WindowStack.Add(new Window_TransitionWorld());
            }
        }
    }

    [HarmonyPatch(typeof(WITab_Planet), MethodType.Constructor)]
    public static class WITab_Planet_Constructor_Patch
    {
        public static void Postfix(WITab_Planet __instance, ref Vector2 ___WinSize)
        {
            ___WinSize = __instance.size = new Vector2(400f, 300f);
            __instance.labelKey = "WB_World";
        }
    }
}
