using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(WITab_Planet), nameof(WITab_Planet.FillTab))]
    public static class WITab_Planet_FillTab_Patch
    {
        public static void Postfix(WITab_Planet __instance, ref Vector2 ___WinSize)
        {
            ___WinSize = __instance.size = new Vector2(400f, 330f);
            __instance.labelKey = "WB_World";
            var rect = new Rect(5f, __instance.size.y - 37f * 6, 150, 32);
            if (ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId))
            {
                rect.y -= 37f;
            }
            if (Widgets.ButtonText(rect, "WB_MapEditorLabel".Translate()))
            {
                Find.WindowStack.Add(new Window_MapEditor());
            }
            rect.y += 37f;
            if (Widgets.ButtonText(rect, "WB_GizmoEditMapTextLabel".Translate()))
            {
                Find.WindowStack.Add(new Window_MapTextEditor());
            }
            rect.y += 37f;
            if (Widgets.ButtonText(rect, "WB_ManageFactionsTitle".Translate()))
            {
                Find.WindowStack.Add(new Window_ManageFactions());
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
                    floatMenuOptions.Add(new FloatMenuOption(localPreset.Label, () => Find.WindowStack.Add(new Window_CreateOrEditWorld(localPreset, enableAllCheckboxes: false, isEditingExistingPreset: true))));
                }
                if (floatMenuOptions.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                }
                else
                {
                    Messages.Message("WB_SelectWorldToEditNoneFound".Translate(), MessageTypeDefOf.RejectInput);
                }
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
            ___WinSize = __instance.size = new Vector2(400f, 330f);
            __instance.labelKey = "WB_World";
        }
    }
}
