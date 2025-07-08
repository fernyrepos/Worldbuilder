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
    [HotSwappable]
    [StaticConstructorOnStartup]
    public static class CustomizationGraphicUtility
    {
        private static readonly Dictionary<string, Texture2D> customTextureCache = new Dictionary<string, Texture2D>();
        private static readonly Texture2D MissingTexture = SolidColorMaterials.NewSolidColorTexture(Color.magenta);
        public static Graphic GetGraphic(ThingDef def, ThingDef stuff, CustomizationData data)
        {
            if (data == null || def?.graphicData == null)
            {
                return def?.graphic;
            }

            Shader shader = def.graphicData.shaderType?.Shader ?? ShaderDatabase.Cutout;
            Vector2 drawSize = def.graphicData.drawSize;

            if (!string.IsNullOrEmpty(data.selectedImagePath))
            {
                string imagePath = data.selectedImagePath;
                if (imagePath.StartsWith("CustomImages/") && WorldPresetManager.CurrentlyLoadedPreset != null)
                {
                    string presetFolder = Path.Combine(GenFilePaths.FolderUnderSaveData("Worldbuilder"), WorldPresetManager.CurrentlyLoadedPreset.name);
                    string relativePath = imagePath.Substring("CustomImages/".Length).Replace('/', Path.DirectorySeparatorChar);
                    imagePath = Path.Combine(presetFolder, relativePath);
                }

                if (customTextureCache.TryGetValue(imagePath, out Texture2D cachedTex))
                {
                    if (cachedTex != MissingTexture)
                    {
                        GraphicRequest req = new GraphicRequest(typeof(Graphic_Single), cachedTex, shader, drawSize, Color.white, Color.white, null, 0, null, null);
                        Graphic_Single graphic = new Graphic_Single();
                        graphic.Init(req);
                        graphic.MatSingle.mainTexture = cachedTex;
                        return graphic;
                    }
                    return null;
                }

                if (File.Exists(imagePath))
                {
                    try
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(File.ReadAllBytes(imagePath));
                        texture.name = Path.GetFileNameWithoutExtension(imagePath);
                        customTextureCache[imagePath] = texture;

                        GraphicRequest req = new GraphicRequest(typeof(Graphic_Single), texture, shader, drawSize, Color.white, Color.white, null, 0, null, null);
                        Graphic_Single graphic = new Graphic_Single();
                        graphic.Init(req);
                        graphic.MatSingle.mainTexture = texture;
                        return graphic;
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorOnce($"Worldbuilder: Failed to load custom image '{imagePath}': {ex.Message}", imagePath.GetHashCode());
                        customTextureCache[imagePath] = MissingTexture;
                        return null;
                    }
                }
                else
                {
                    Log.WarningOnce($"Worldbuilder: Custom image path not found: {imagePath}", imagePath.GetHashCode());
                    customTextureCache[imagePath] = MissingTexture;
                    return null;
                }
            }
            var color = data.color ?? (def.MadeFromStuff ? def.GetColorForStuff(stuff) : def.uiIconColor);
            if (data.variationIndex.HasValue)
            {
                var compProperties = def.GetCompProperties<CompProperties_RandomBuildingGraphic>();
                if (compProperties?.randomGraphics != null && data.variationIndex.Value >= 0 && data.variationIndex.Value < compProperties.randomGraphics.Count)
                {
                    string variationPath = compProperties.randomGraphics[data.variationIndex.Value];
                    if (!string.IsNullOrEmpty(variationPath))
                    {
                        try
                        {
                            return GraphicDatabase.Get(def.graphicData.graphicClass, variationPath, shader, drawSize, color, Color.white, null);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorOnce($"Worldbuilder: Failed to load variation graphic '{variationPath}': {ex.Message}", variationPath.GetHashCode());
                            return null;
                        }
                    }
                }
                Log.WarningOnce($"Worldbuilder: Invalid variation index {data.variationIndex.Value} for {def.defName}", def.GetHashCode() ^ data.variationIndex.Value);
                return null;
            }

            if (data.styleDef?.graphicData != null)
            {
                return data.styleDef.graphicData.Graphic.GetColoredVersion(data.styleDef.graphicData.Graphic.Shader, color, Color.white);
            }
            return def.graphic;
        }

        public static void DrawCustomizedGraphicFor(Rect rect, ThingDef def, ThingDef stuff, CustomizationData data, float iconAngle = 0f, float iconDrawScale = 1f, Color? overrideColor = null)
        {
            if (def == null) return;
            Graphic graphic = GetGraphic(def, stuff, data);
            bool useDefIcon = (graphic is Graphic_Linked) || (def.thingClass.IsAssignableFrom(typeof(Building_Door)) && data?.styleDef != null);
            var dataColor = overrideColor ?? data?.color;
            var color = dataColor ?? (def.MadeFromStuff ? def.GetColorForStuff(stuff) : def.uiIconColor);
            if (useDefIcon)
            {   
                ThingStyleDef styleToUse = (data?.styleDef != null && string.IsNullOrEmpty(data.selectedImagePath) && !data.variationIndex.HasValue) ? data.styleDef : null;
                Widgets.DefIcon(rect, def, stuff, iconDrawScale * 0.85f, styleToUse, false);
            }
            else if (graphic != null)
            {
                GUI.color = color;
                Material material = graphic.MatAt(def.defaultPlacingRot);
                Texture resolvedTexture = material.mainTexture;
                Widgets.ThingIconWorker(rect, def, resolvedTexture, iconAngle, iconDrawScale * 0.85f);
                GUI.color = Color.white;
            }
            else
            {

                Widgets.DefIcon(rect, def, stuff, iconDrawScale * 0.85f, null, false, color);
            }
        }
    }
}
