using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.IO;
using System.Collections.Generic;
using VEF.Buildings;
using System.Linq;

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
                string imagePath = data.ResolvedImagePath;

                if (customTextureCache.TryGetValue(imagePath, out Texture2D cachedTex))
                {
                    if (cachedTex != MissingTexture)
                    {
                        var req = new GraphicRequest(typeof(Graphic_Single), cachedTex, shader, drawSize, Color.white, Color.white, null, 0, null, null);
                        var graphic = new Graphic_Single();
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
                        var texture = new Texture2D(2, 2);
                        texture.LoadImage(File.ReadAllBytes(imagePath));
                        texture.name = Path.GetFileNameWithoutExtension(imagePath);
                        customTextureCache[imagePath] = texture;

                        var req = new GraphicRequest(typeof(Graphic_Single), texture, shader, drawSize, Color.white, Color.white, null, 0, null, null);
                        var graphic = new Graphic_Single();
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
            var graphic = GetGraphic(def, stuff, data);
            bool useDefIcon = graphic is Graphic_Linked || (typeof(Building_Door).IsAssignableFrom(def.thingClass) && data?.styleDef != null);
            var dataColor = overrideColor ?? data?.color;
            var color = dataColor ?? (def.MadeFromStuff ? def.GetColorForStuff(stuff) : def.uiIconColor);

            float totalAngle = iconAngle + (data?.rotation ?? 0f);

            Vector2 texProportions = def.graphicData?.drawSize ?? Vector2.one;
            float fitScale = (texProportions.x / texProportions.y < rect.width / rect.height)
                ? (rect.height / texProportions.y)
                : (rect.width / texProportions.x);
            fitScale *= iconDrawScale * 0.85f;

            Rect drawRect = rect;

            if (data != null)
            {
                drawRect.x += data.drawOffset.x * fitScale;
                drawRect.y -= data.drawOffset.y * fitScale;
            }

            if (useDefIcon)
            {
                ThingStyleDef styleToUse = (data?.styleDef != null && string.IsNullOrEmpty(data.selectedImagePath) && !data.variationIndex.HasValue) ? data.styleDef : null;
                Widgets.DefIcon(drawRect, def, stuff, iconDrawScale * 0.85f, styleToUse, false, color);
            }
            else if (graphic != null)
            {
                GUI.color = color;
                Material material;
                if (graphic is Graphic_Random random && data?.randomIndexOverride != null && data.randomIndexOverride.TryGetValue(data.RandomIndexKey, out int index) && index >= 0 && index < random.subGraphics.Length)
                {
                    material = random.subGraphics[index].MatAt(Rot4.South);
                }
                else
                {
                    material = graphic is Graphic_Random random2 ? random2.subGraphics.First().MatAt(Rot4.South) : graphic.MatAt(Rot4.South);
                }
                Texture resolvedTexture = material.mainTexture;

                Vector2 iconTexProportions = new Vector2(resolvedTexture.width, resolvedTexture.height);
                if (def.graphicData != null)
                {
                    iconTexProportions = def.graphicData.drawSize;
                }

                Rect iconRect = new Rect(0f, 0f, iconTexProportions.x, iconTexProportions.y);
                float aspect = iconRect.width / iconRect.height;
                float outerAspect = drawRect.width / drawRect.height;
                float scaleFactor = (aspect < outerAspect) ? (drawRect.height / iconRect.height) : (drawRect.width / iconRect.width);
                scaleFactor *= iconDrawScale * 0.85f;

                iconRect.width *= scaleFactor;
                iconRect.height *= scaleFactor;
                iconRect.center = drawRect.center;

                if (totalAngle != 0f)
                {
                    Matrix4x4 m = Matrix4x4.TRS(iconRect.center, Quaternion.Euler(0f, 0f, totalAngle), Vector3.one) * Matrix4x4.TRS(-iconRect.center, Quaternion.identity, Vector3.one);
                    GL.PushMatrix();
                    GL.MultMatrix(m);
                    GUI.DrawTexture(iconRect, resolvedTexture);
                    GL.PopMatrix();
                }
                else
                {
                    GUI.DrawTexture(iconRect, resolvedTexture);
                }

                GUI.color = Color.white;
            }
            else
            {
                Widgets.DefIcon(drawRect, def, stuff, iconDrawScale * 0.85f, null, false, color);
            }
        }
    }
}
