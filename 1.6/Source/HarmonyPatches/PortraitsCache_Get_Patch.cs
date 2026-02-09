using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
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
            if (!CustomizationDataCollections.thingCustomizationData.TryGetValue(pawn, out var data) || !data.displayCustomPortraitInColonistBar)
            {
                return true;
            }

            string resolvedPath = data.ResolvedImagePath;
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
            {
                return true;
            }

            float pawnScale = cameraZoom / ColonistBarColonistDrawer.PawnTextureCameraZoom;

            string cacheKey = $"{pawn.ThingID}_{size.x}x{size.y}_{resolvedPath.GetHashCode()}_{pawnScale:F2}_{cameraOffset.x:F2}_{cameraOffset.z:F2}";

            if (customPortraitCache.TryGetValue(cacheKey, out var cachedTexture))
            {
                if (cachedTexture != null && cachedTexture.IsCreated())
                {
                    __result = cachedTexture;
                    return false;
                }
                customPortraitCache.Remove(cacheKey);
            }

            __result = GetCustomPortraitAsRenderTexture(pawn, resolvedPath, size, pawnScale, cameraOffset, supersample);

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
                if (key.StartsWith(pawn.ThingID)) keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
            {
                if (customPortraitCache.TryGetValue(key, out var tex) && tex != null) tex.Release();
                customPortraitCache.Remove(key);
            }
        }

        private static RenderTexture GetCustomPortraitAsRenderTexture(Pawn pawn, string imagePath, Vector2 size, float pawnScale, Vector3 cameraOffset, bool supersample)
        {
            try
            {
                Texture2D customTex = Window_PawnCustomization.GetTextureForPreview(imagePath);
                if (customTex == null) return null;

                customTex.filterMode = FilterMode.Point;
                customTex.anisoLevel = 0;
                customTex.wrapMode = TextureWrapMode.Clamp;

                int superSampleFactor = (size.x > 150 || size.y > 150) ? 2 : 1;
                if (size.x > 300) superSampleFactor = 3;

                int renderWidth = (int)(size.x * superSampleFactor);
                int renderHeight = (int)(size.y * superSampleFactor);

                RenderTexture renderTexture = new RenderTexture(
                    renderWidth,
                    renderHeight,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default)
                {
                    name = $"CustomPortrait_{pawn.LabelShort}",
                    useMipMap = true,
                    autoGenerateMips = true,
                    antiAliasing = 4,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0,
                    wrapMode = TextureWrapMode.Clamp
                };

                renderTexture.Create();

                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = renderTexture;

                GL.Clear(true, true, Color.clear);
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, renderWidth, renderHeight, 0);

                float sourceAspect = (float)customTex.width / customTex.height;
                float targetAspect = size.x / size.y;

                float baseWidth, baseHeight;
                if (sourceAspect > targetAspect)
                {
                    baseWidth = renderWidth;
                    baseHeight = baseWidth / sourceAspect;
                }
                else
                {
                    baseHeight = renderHeight;
                    baseWidth = baseHeight * sourceAspect;
                }

                float drawW = baseWidth * pawnScale;
                float drawH = baseHeight * pawnScale;

                float centerX = renderWidth / 2f;
                float centerY = renderHeight / 2f;

                float unitToPixels = renderHeight / ColonistBarColonistDrawer.PawnTextureCameraZoom;
                centerX -= cameraOffset.x * unitToPixels;
                centerY += cameraOffset.z * unitToPixels;

                float finalX = centerX - (drawW / 2f);
                float finalY = centerY - (drawH / 2f);

                Graphics.DrawTexture(new Rect(finalX, finalY, drawW, drawH), customTex);

                GL.PopMatrix();
                RenderTexture.active = previousActive;

                return renderTexture;
            }
            catch (Exception ex)
            {
                Log.Error($"Worldbuilder: Failed to create custom portrait for {pawn.LabelShort}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}
