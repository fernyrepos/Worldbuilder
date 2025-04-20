using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.IO;
using System.Collections.Generic;
using VanillaFurnitureExpanded;

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
        private static readonly Dictionary<string, Texture2D> designatorIconTextureCache = new Dictionary<string, Texture2D>();
        private static Graphic TryGetWorldbuilderDefaultGraphic(ThingDef def, CustomizationData defaultData)
        {
            if (def?.graphicData == null) return null;

            Shader shader = def.graphicData.shaderType?.Shader ?? ShaderDatabase.Cutout;
            Vector2 drawSize = def.graphicData.drawSize;
            if (!string.IsNullOrEmpty(defaultData.selectedImagePath))
            {
                string imagePath = defaultData.selectedImagePath;
                if (designatorIconTextureCache.TryGetValue(imagePath, out Texture2D cachedTex))
                {
                    GraphicRequest req = new GraphicRequest(typeof(Graphic_Single), cachedTex, shader, drawSize, Color.white, Color.white, null, 0, null, null);
                    Graphic_Single graphic = new Graphic_Single();
                    graphic.Init(req);
                    graphic.MatSingle.mainTexture = cachedTex;
                    return graphic;
                }
                if (File.Exists(imagePath))
                {
                    try
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(File.ReadAllBytes(imagePath));
                        texture.name = Path.GetFileNameWithoutExtension(imagePath);
                        designatorIconTextureCache[imagePath] = texture;
                        GraphicRequest req = new GraphicRequest(typeof(Graphic_Single), texture, shader, drawSize, Color.white, Color.white, null, 0, null, null);
                        Graphic_Single graphic = new Graphic_Single();
                        graphic.Init(req);
                        graphic.MatSingle.mainTexture = texture;
                        return graphic;
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorOnce($"Worldbuilder: Failed to load custom image '{imagePath}' for designator icon: {ex.Message}", imagePath.GetHashCode());
                        return null;
                    }
                }
                else
                {
                    Log.WarningOnce($"Worldbuilder: Custom image path not found for designator icon: {imagePath}", imagePath.GetHashCode());
                    return null;
                }
            }
            if (defaultData.variationIndex.HasValue)
            {
                var compProperties = def.GetCompProperties<CompProperties_RandomBuildingGraphic>();
                if (compProperties?.randomGraphics != null && defaultData.variationIndex.Value >= 0 && defaultData.variationIndex.Value < compProperties.randomGraphics.Count)
                {
                    string variationPath = compProperties.randomGraphics[defaultData.variationIndex.Value];
                    if (!string.IsNullOrEmpty(variationPath))
                    {
                        try
                        {
                            return GraphicDatabase.Get(def.graphicData.graphicClass, variationPath, shader, drawSize, Color.white, Color.white, null);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorOnce($"Worldbuilder: Failed to load variation graphic '{variationPath}' for designator icon: {ex.Message}", variationPath.GetHashCode());
                            return null;
                        }
                    }
                }
                Log.WarningOnce($"Worldbuilder: Invalid variation index {defaultData.variationIndex.Value} for {def.defName}", def.GetHashCode() ^ defaultData.variationIndex.Value);
                return null;
            }
            if (defaultData.styleDef?.graphicData != null)
            {
                return defaultData.styleDef.graphicData.Graphic;
            }
            return null;
        }

        public static bool Prefix(Designator_Build __instance, Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var defaultData))
                {
                    Graphic customGraphic = TryGetWorldbuilderDefaultGraphic(def, defaultData);

                    if (customGraphic != null)
                    {
                        Color color = parms.lowLight ? Command.LowLightIconColor : __instance.IconDrawColor;
                        Material material = customGraphic.MatAt(def.defaultPlacingRot);

                        if (material != null)
                        {
                            Texture resolvedTexture = material.mainTexture;
                            if (resolvedTexture != null)
                            {
                                GUI.color = color;
                                Widgets.ThingIconWorker(
                                    rect,
                                    def,
                                    resolvedTexture,
                                    __instance.iconAngle,
                                    __instance.iconDrawScale * 0.85f
                                );
                                GUI.color = Color.white;
                                return false;
                            }
                        }
                    }
                }
            }
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
