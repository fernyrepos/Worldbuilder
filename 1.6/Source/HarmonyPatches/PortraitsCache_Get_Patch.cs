using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(PortraitsCache), nameof(PortraitsCache.Get))]
    public static class PortraitsCache_Get_Patch
    {
        private static readonly Dictionary<string, RenderTexture> customPortraitCache = new Dictionary<string, RenderTexture>();

        public static bool Prefix(
            Pawn pawn,
            Vector2 size,
            Rot4 rotation,
            Vector3 cameraOffset,
            float cameraZoom,
            bool supersample,
            bool compensateForUIScale,
            bool renderHeadgear,
            bool renderClothes,
            IReadOnlyDictionary<Apparel, Color> overrideApparelColors,
            Color? overrideHairColor,
            bool stylingStation,
            PawnHealthState? healthStateOverride,
            ref RenderTexture __result)
        {
            if (!CustomizationDataCollections.thingCustomizationData.TryGetValue(pawn, out var data))
            {
                return true;
            }

            if (!data.displayCustomPortraitInColonistBar)
            {
                return true;
            }

            string imagePath = data.ResolvedImagePath;
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return true;
            }

            string cacheKey = $"{pawn.ThingID}_{(int)size.x}x{(int)size.y}_{imagePath.GetHashCode()}";

            if (customPortraitCache.TryGetValue(cacheKey, out var cachedTexture))
            {
                if (cachedTexture != null && cachedTexture.IsCreated())
                {
                    __result = cachedTexture;
                    return false;
                }
                else
                {
                    customPortraitCache.Remove(cacheKey);
                }
            }

            __result = GetCustomPortraitAsRenderTexture(pawn, imagePath, size);

            if (__result != null)
            {
                customPortraitCache[cacheKey] = __result;
            }

            return __result == null;
        }

        public static void InvalidateCache(Pawn pawn)
        {
            var keysToRemove = new List<string>();
            foreach (var key in customPortraitCache.Keys)
            {
                if (key.StartsWith(pawn.ThingID))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                if (customPortraitCache.TryGetValue(key, out var tex) && tex != null)
                {
                    tex.Release();
                }
                customPortraitCache.Remove(key);
            }
        }

        private static RenderTexture GetCustomPortraitAsRenderTexture(Pawn pawn, string imagePath, Vector2 size)
        {
            try
            {
                Texture2D customTex = Window_PawnCustomization.GetTextureForPreview(imagePath);
                if (customTex == null)
                {
                    return null;
                }

                RenderTexture renderTexture = new RenderTexture((int)size.x, (int)size.y, 24, RenderTextureFormat.ARGB32)
                {
                    name = $"CustomPortrait_{pawn.LabelShort}",
                    antiAliasing = 4,
                    filterMode = FilterMode.Bilinear
                };

                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.clear);

                GL.PushMatrix();
                GL.LoadPixelMatrix(0, size.x, size.y, 0);

                float sourceAspect = (float)customTex.width / customTex.height;
                float targetAspect = size.x / size.y;
                
                float drawWidth = size.x;
                float drawHeight = size.x / sourceAspect;
                float yOffset = size.y - drawHeight;
                
                if (drawHeight > size.y)
                {
                    drawHeight = size.y;
                    drawWidth = size.y * sourceAspect;
                    float xOffset = (size.x - drawWidth) / 2f;
                    yOffset = 0;
                    Graphics.DrawTexture(new Rect(xOffset, yOffset, drawWidth, drawHeight), customTex);
                }
                else
                {
                    Graphics.DrawTexture(new Rect(0, yOffset, drawWidth, drawHeight), customTex);
                }

                GL.PopMatrix();
                RenderTexture.active = null;

                return renderTexture;
            }
            catch (Exception ex)
            {
                Log.Error($"Worldbuilder: Failed to create custom portrait RenderTexture for {pawn.LabelShort}: {ex.Message}");
                return null;
            }
        }
    }
}
