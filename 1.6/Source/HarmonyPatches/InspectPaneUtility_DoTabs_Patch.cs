using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using System.IO;
using System.Collections.Generic;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(InspectPaneUtility), "DoTabs")]
    public static class InspectPaneUtility_DoTabs_Patch
    {
        private static readonly Dictionary<string, Texture2D> portraitTextureCache = new Dictionary<string, Texture2D>();
        private static readonly Texture2D MissingTexture = SolidColorMaterials.NewSolidColorTexture(Color.magenta);

        public static void Prefix(IInspectPane pane)
        {
            if (!WorldRendererUtility.DrawingMap) return;

            if (!(Find.UIRoot is UIRoot_Play))
            {
                return;
            }
            if (Find.Selector.NumSelected == 1 && Find.Selector.SingleSelectedThing is Pawn pawn)
            {
                var customizationData = pawn.GetCustomizationData();
                if (customizationData != null && !string.IsNullOrEmpty(customizationData.selectedImagePath))
                {
                    string imagePathToLoad = customizationData.selectedImagePath;
                    if (imagePathToLoad.StartsWith("CustomImages/") && WorldPresetManager.CurrentlyLoadedPreset != null)
                    {
                        imagePathToLoad = Path.Combine(WorldPresetManager.CurrentlyLoadedPreset.presetFolder, imagePathToLoad.Replace('/', Path.DirectorySeparatorChar));
                    }

                    if (File.Exists(imagePathToLoad))
                    {
                        Texture2D customPortrait = GetTextureForInspectPane(imagePathToLoad);
                        if (customPortrait != null && customPortrait != MissingTexture)
                        {
                            float tabsTopY = pane.PaneTopY - 30f;
                            float aspectRatio = (float)customPortrait.width / customPortrait.height;
                            float portraitSize = WorldbuilderMod.settings.pawnPortraitSize;
                            float calculatedWidth = portraitSize * aspectRatio;
                            Rect portraitRect = new Rect(12f, tabsTopY - portraitSize, calculatedWidth, portraitSize);
                            GUI.color = Color.white;
                            GUI.DrawTexture(portraitRect, customPortrait, ScaleMode.ScaleToFit);
                            GUI.color = Color.white;
                        }
                    }
                }
            }
        }
        private static Texture2D GetTextureForInspectPane(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            if (portraitTextureCache.TryGetValue(path, out Texture2D cachedTex))
            {
                return cachedTex;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D newTex = new Texture2D(2, 2);
                if (newTex.LoadImage(fileData))
                {
                    portraitTextureCache[path] = newTex;
                    return newTex;
                }
                portraitTextureCache[path] = MissingTexture;
                return MissingTexture;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Exception loading texture from {path} for inspect pane portrait: {ex.Message}");
                portraitTextureCache[path] = MissingTexture;
                return MissingTexture;
            }
        }
    }
}
