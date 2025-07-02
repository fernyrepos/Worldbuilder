using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.IO;
using System.Collections.Generic;
using VEF.Buildings;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.IconDrawColor), MethodType.Getter)]
    public static class Designator_Build_IconDrawColor_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref Color __result)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                CustomizationData defaultData = null;
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var playerData))
                {
                    defaultData = playerData;
                }
                Color baseColor = __instance.StuffDef != null ? def.GetColorForStuff(__instance.StuffDef) : def.uiIconColor;
                bool isBaseColor = Mathf.Approximately(__result.r, baseColor.r) &&
                                   Mathf.Approximately(__result.g, baseColor.g) &&
                                   Mathf.Approximately(__result.b, baseColor.b) &&
                                   Mathf.Approximately(__result.a, baseColor.a);

                if (defaultData?.color != null && isBaseColor)
                {
                    Color? ideoColor = IdeoUtility.GetIdeoColorForBuilding(def, Faction.OfPlayer);
                    if (!ideoColor.HasValue)
                    {
                        __result = defaultData.color.Value;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.DrawIcon))]
    public static class Designator_Build_DrawIcon_Patch
    {
        // Removed TryGetWorldbuilderDefaultGraphic and cache, logic moved to CustomizationGraphicUtility

        public static bool Prefix(Designator_Build __instance, Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                // Check player defaults first
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var defaultData))
                {
                    // Use the utility to draw the icon based on the default data
                    // The utility handles custom images, variations, styles, and color internally
                    Color color = parms.lowLight ? Command.LowLightIconColor : __instance.IconDrawColor; // Get potentially patched color
                    CustomizationGraphicUtility.DrawCustomizedGraphicFor(
                        rect,
                        def,
                        defaultData,
                        __instance.iconAngle,
                        __instance.iconDrawScale,
                        color // Pass the potentially patched color as override
                    );
                    return false; // Skip original DrawIcon as we've drawn the customized version
                }
                // If no player default, fall through to original DrawIcon (return true)
                // The original DrawIcon will use patched IconDrawColor and ThingStyleDefForPreview if applicable (e.g., Ideology styles)
            }
            // Let original DrawIcon handle non-ThingDefs or cases without relevant WB defaults
            return true;
        }
    }
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.ThingStyleDefForPreview), MethodType.Getter)]
    public static class Designator_Build_ThingStyleDefForPreview_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref ThingStyleDef __result)
        {
            if (__result == null && __instance.PlacingDef is ThingDef def)
            {
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var defaultData))
                {
                    if (defaultData?.styleDef != null && string.IsNullOrEmpty(defaultData.selectedImagePath) && !defaultData.variationIndex.HasValue)
                    {
                        __result = defaultData.styleDef;
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Label), MethodType.Getter)]
    public static class Designator_Build_Label_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref string __result)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var defaultData))
                {
                    if (!string.IsNullOrEmpty(defaultData.labelOverride))
                    {
                        bool writeStuffValue = Traverse.Create(__instance).Field<bool>("writeStuff").Value;
                        bool useBaseLabel = (def == null || !writeStuffValue);

                        if (useBaseLabel)
                        {
                            string newLabel = defaultData.labelOverride;
                            if (__instance.sourcePrecept != null)
                            {
                                newLabel = __instance.sourcePrecept.TransformThingLabel(newLabel);
                            }
                            if (def != null && !writeStuffValue && def.MadeFromStuff)
                            {
                                newLabel += "...";
                            }

                            __result = newLabel;
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Desc), MethodType.Getter)]
    public static class Designator_Build_Desc_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref string __result)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var defaultData))
                {
                    if (!string.IsNullOrEmpty(defaultData.descriptionOverride))
                    {
                        __result = defaultData.descriptionOverride;
                    }
                }
            }
        }
    }
}
