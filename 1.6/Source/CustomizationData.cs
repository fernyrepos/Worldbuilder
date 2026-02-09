using Verse;
using UnityEngine;
using VEF.Buildings;
using System.IO;
using System.Collections.Generic;
using RimWorld;
using System;

namespace Worldbuilder
{
    [HotSwappable]
    public class CustomizationData : IExposable
    {
        public CustomizationData() { }
        public Color? color;
        private ThingStyleDef _styleDef;
        public ThingStyleDef styleDef
        {
            get => _styleDef;
            set
            {
                _styleDef = value;
                if (value != null)
                {
                    selectedImagePath = null;
                    variationIndex = null;
                }
            }
        }
        public string labelOverride;
        public string descriptionOverride;
        public string narrativeText;
        private int? _variationIndex;
        public int? variationIndex
        {
            get => _variationIndex;
            set
            {
                _variationIndex = value;
                if (value.HasValue)
                {
                    selectedImagePath = null;
                    styleDef = null;
                }
            }
        }
        private string _selectedImagePath;
        public string selectedImagePath
        {
            get => _selectedImagePath;
            set
            {
                _selectedImagePath = value;
                if (!string.IsNullOrEmpty(value))
                {
                    variationIndex = null;
                    styleDef = null;
                }
            }
        }
        public bool includeAdditionalDetails = true;
        public bool includeMaterialInLabel = true;
        public ThingStyleDef originalStyleDef;
        private static readonly Dictionary<GraphicCacheKey, Graphic> graphicCache = new Dictionary<GraphicCacheKey, Graphic>();
        private static readonly Dictionary<GraphicCacheKey, Graphic> graphicCacheDef = new Dictionary<GraphicCacheKey, Graphic>();
        public Name nameOverride;
        public Name originalPawnName;
        public float rotation = 0f;
        public Vector2 drawOffset = Vector2.zero;
        public AltitudeLayer? altitudeLayer;
        public bool displayCustomPortraitInColonistBar = false;
        public string ResolvedImagePath
        {
            get
            {
                if (string.IsNullOrEmpty(selectedImagePath))
                {
                    return null;
                }
                if (selectedImagePath.StartsWith(WorldPreset.CustomImagesFolderName) && WorldPresetManager.CurrentlyLoadedPreset != null)
                {
                    var relativePath = selectedImagePath.Substring(WorldPreset.CustomImagesFolderName.Length + 1);
                    return Path.Combine(WorldPresetManager.CurrentlyLoadedPreset.CustomImagesPath, relativePath);
                }
                return selectedImagePath;
            }
        }
        public bool IsExternalImage => !string.IsNullOrEmpty(selectedImagePath) && !selectedImagePath.StartsWith(WorldPreset.CustomImagesFolderName) && File.Exists(selectedImagePath);
        public string RandomIndexKey => styleDef?.defName ?? "WB_Default";
        public Dictionary<string, int> randomIndexOverride;

        public void ExposeData()
        {
            Scribe_Values.Look(ref color, "color", Color.white);
            Scribe_Defs.Look(ref _styleDef, "styleDef");
            Scribe_Values.Look(ref labelOverride, "labelOverride");
            Scribe_Values.Look(ref descriptionOverride, "descriptionOverride");
            Scribe_Values.Look(ref narrativeText, "narrativeText");
            Scribe_Values.Look(ref _variationIndex, "variationIndex");
            Scribe_Values.Look(ref _selectedImagePath, "selectedImagePath");
            Scribe_Values.Look(ref includeAdditionalDetails, "includeAdditionalDetails", defaultValue: true);
            Scribe_Values.Look(ref includeMaterialInLabel, "includeMaterialInLabel", defaultValue: true);
            Scribe_Defs.Look(ref originalStyleDef, "originalStyleDef");
            Scribe_Deep.Look(ref nameOverride, "nameOverride");
            Scribe_Deep.Look(ref originalPawnName, "originalPawnName");
            Scribe_Collections.Look(ref randomIndexOverride, "randomIndexOverride", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref rotation, "rotation", 0f);
            Scribe_Values.Look(ref drawOffset, "drawOffset", Vector2.zero);
            Scribe_Values.Look(ref altitudeLayer, "altitudeLayer");
            Scribe_Values.Look(ref displayCustomPortraitInColonistBar, "displayCustomPortraitInColonistBar", defaultValue: false);
        }

        public Graphic DefaultGraphic(Thing thing)
        {
            ThingStyleDef styleDef = thing.StyleDef;
            if (styleDef?.Graphic != null)
            {
                if (styleDef.graphicData != null)
                {
                    return styleDef.graphicData.GraphicColoredFor(thing);
                }
                else
                {
                    return styleDef.Graphic;
                }
            }
            if (thing.def.graphicData == null)
            {
                return BaseContent.BadGraphic;
            }
            return thing.def.graphicData.GraphicColoredFor(thing);
        }

        private static bool gettingGraphic;
        public Graphic GetGraphic(Thing thing)
        {
            if (gettingGraphic) return null;
            gettingGraphic = true;
            try
            {
                var result = GetGraphicInner(thing);
                gettingGraphic = false;
                return result;
            }
            catch (Exception ex)
            {
                gettingGraphic = false;
                Log.Error("Caught exception: " + ex + " with getting customized graphic from " + thing);
                return thing.Graphic;
            }
        }

        private Graphic GetGraphicInner(Thing thing)
        {
            var def = thing.def;
            var stuff = thing.Stuff;
            var colorTwo = thing.DrawColorTwo;
            if (def.graphicData is null)
            {
                Log.Error("No graphic data found for " + def);
                return thing.Graphic;
            }
            if (def.graphicData.shaderType is null)
            {
                Log.Error("No shader found for " + def);
                return thing.Graphic;
            }
            Shader shader = def.graphicData.shaderType.Shader;
            if (styleDef != null)
            {
                shader = styleDef.graphicData.shaderType.Shader;
            }
            var key = new GraphicCacheKey(color, colorTwo, styleDef, variationIndex, selectedImagePath, def, stuff, rotation, drawOffset, altitudeLayer);
            if (graphicCache.TryGetValue(key, out Graphic resultGraphic))
            {
                thing.LogMessage("Result graphic: " + resultGraphic + " - styleDef: " + styleDef);
                return resultGraphic;
            }

            Color graphicColor = this.color ?? thing.DrawColor;
            thing.LogMessage("color: " + graphicColor);
            var compProperties = def.CompDefFor<CompRandomBuildingGraphic>();
            bool isCustom = false;

            string resolvedImagePath = ResolvedImagePath;
            Graphic baseGraphic = null;
            if (!string.IsNullOrEmpty(resolvedImagePath))
            {
                if (File.Exists(resolvedImagePath))
                {
                    baseGraphic = CreateCustomGraphic(resolvedImagePath, def, graphicColor);
                }
                else
                {
                    Log.Warning("Custom image not found: " + resolvedImagePath);
                }
                isCustom = true;
            }
            else if (variationIndex.HasValue && compProperties != null && compProperties is CompProperties_RandomBuildingGraphic randomBuildingGraphicProps)
            {
                if (randomBuildingGraphicProps.randomGraphics != null && variationIndex >= 0 && variationIndex < randomBuildingGraphicProps.randomGraphics.Count)
                {
                    string variationPath = randomBuildingGraphicProps.randomGraphics[variationIndex.Value];
                    if (!string.IsNullOrEmpty(variationPath))
                    {
                        baseGraphic = GraphicDatabase.Get(def.graphicData.graphicClass, variationPath, shader, def.graphicData.drawSize, Color.white, Color.white);
                        isCustom = true;
                    }
                }
            }
            else if (styleDef != null && styleDef.graphicData != null)
            {
                baseGraphic = styleDef.graphicData.Graphic;
                isCustom = true;
            }
            thing.LogMessage("Result graphic: " + baseGraphic + " - styleDef: " + styleDef);

            if (baseGraphic == null)
            {
                baseGraphic = thing.Graphic;
                if (graphicColor != Color.white)
                {
                    baseGraphic = baseGraphic.GetColoredVersion(baseGraphic.Shader, graphicColor, thing.DrawColorTwo);
                }
            }
            else if (isCustom && graphicColor != Color.white && (string.IsNullOrEmpty(selectedImagePath) || !File.Exists(resolvedImagePath)))
            {
                baseGraphic = baseGraphic.GetColoredVersion(baseGraphic.Shader, graphicColor, thing.DrawColorTwo);
            }

            Graphic finalGraphic = baseGraphic;
            bool hasTransform = rotation != 0f || drawOffset != Vector2.zero || altitudeLayer.HasValue;

            if (hasTransform && baseGraphic != null)
            {
                finalGraphic = new Graphic_Customized(baseGraphic, this);
            }

            if (isCustom || hasTransform)
            {
                graphicCache[key] = finalGraphic;
            }

            return finalGraphic;
        }

        public Graphic GetGraphicForDef(ThingDef def, ThingDef stuff)
        {
            if (def.graphicData is null)
            {
                Log.Error("No graphic data found for " + def);
                return null;
            }
            if (def.graphicData.shaderType is null)
            {
                Log.Error("No shader found for " + def);
                return null;
            }
            var key = new GraphicCacheKey(color, Color.white, styleDef, variationIndex, selectedImagePath, def, stuff, rotation, drawOffset, altitudeLayer);
            if (graphicCacheDef.TryGetValue(key, out Graphic resultGraphic))
            {
                return resultGraphic;
            }

            Color graphicColor = this.color ?? Color.white;

            Shader shader = def.graphicData.shaderType.Shader;
            var compProperties = def.CompDefFor<CompRandomBuildingGraphic>();
            bool isCustom = false;

            string resolvedImagePath = ResolvedImagePath;
            Graphic baseGraphic = null;
            if (!string.IsNullOrEmpty(resolvedImagePath))
            {
                if (File.Exists(resolvedImagePath))
                {
                    baseGraphic = CreateCustomGraphic(resolvedImagePath, def, graphicColor);
                    isCustom = true;
                }
            }
            else if (variationIndex.HasValue && compProperties != null && compProperties is CompProperties_RandomBuildingGraphic randomBuildingGraphicProps)
            {
                if (randomBuildingGraphicProps.randomGraphics != null && variationIndex >= 0 && variationIndex < randomBuildingGraphicProps.randomGraphics.Count)
                {
                    string variationPath = randomBuildingGraphicProps.randomGraphics[variationIndex.Value];
                    if (!string.IsNullOrEmpty(variationPath))
                    {
                        baseGraphic = GraphicDatabase.Get(def.graphicData.graphicClass, variationPath, shader, def.graphicData.drawSize, Color.white, Color.white);
                        isCustom = true;
                    }
                }
            }
            else if (styleDef != null && styleDef.graphicData != null)
            {
                baseGraphic = styleDef.graphicData.Graphic;
                isCustom = true;
            }

            if (baseGraphic == null)
            {

            }
            else if (isCustom && graphicColor != Color.white && (string.IsNullOrEmpty(selectedImagePath) || !File.Exists(resolvedImagePath)))
            {
                baseGraphic = baseGraphic.GetColoredVersion(baseGraphic.Shader, graphicColor, Color.white);
            }

            Graphic finalGraphic = baseGraphic;
            bool hasTransform = rotation != 0f || drawOffset != Vector2.zero || altitudeLayer.HasValue;

            if (hasTransform && baseGraphic != null)
            {
                finalGraphic = new Graphic_Customized(baseGraphic, this);
            }

            if (isCustom || hasTransform)
            {
                graphicCacheDef[key] = finalGraphic;
            }

            return finalGraphic;
        }

        private Graphic CreateCustomGraphic(string filePath, ThingDef def, Color color)
        {
            var fileData = File.ReadAllBytes(filePath);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);

            Vector2 graphicDrawSize = def.graphicData.drawSize;
            Shader shader = def.graphicData.shaderType.Shader;
            var graphic = GetInner<Graphic_Single>(new GraphicRequest(typeof(Graphic_Single),
            texture, shader,
            graphicDrawSize, color, Color.white, null, 0, null, null));
            graphic.MatSingle.mainTexture = texture;
            return graphic;
        }

        private static T GetInner<T>(GraphicRequest req) where T : Graphic, new()
        {
            req.color = (Color32)req.color;
            req.colorTwo = (Color32)req.colorTwo;
            req.renderQueue = ((req.renderQueue == 0 && req.graphicData != null) ? req.graphicData.renderQueue : req.renderQueue);
            var value = new T();
            value.Init(req);
            return (T)value;
        }

        public void SetGraphic(Thing thing)
        {
            thing.StyleDef = null;
            thing.graphicInt = null;
            thing.styleGraphicInt = null;
            thing.graphicInt = GetGraphic(thing);
            thing.styleGraphicInt = thing.graphicInt;
            thing.LogMessage("Set graphic, thing.graphicInt: " + thing.graphicInt);
            CustomizationDataCollections.explicitlyCustomizedThings.Add(thing);
            if (thing.Spawned)
            {
                var map = thing.Map;
                thing.DirtyMapMesh(map);

                foreach (var offset in GenAdj.CardinalDirections)
                {
                    var neighborPos = thing.Position + offset;
                    if (neighborPos.InBounds(map))
                    {
                        map.mapDrawer.MapMeshDirty(neighborPos, MapMeshFlagDefOf.Things | MapMeshFlagDefOf.Buildings);
                    }
                }
            }
        }

        public CustomizationData Copy()
        {
            return new CustomizationData()
            {
                color = this.color,
                styleDef = this.styleDef,
                labelOverride = this.labelOverride,
                descriptionOverride = this.descriptionOverride,
                narrativeText = this.narrativeText,
                variationIndex = this.variationIndex,
                selectedImagePath = this.selectedImagePath,
                includeAdditionalDetails = this.includeAdditionalDetails,
                includeMaterialInLabel = this.includeMaterialInLabel,
                originalStyleDef = this.originalStyleDef,
                nameOverride = this.nameOverride,
                originalPawnName = this.originalPawnName,
                randomIndexOverride = this.randomIndexOverride != null ? new Dictionary<string, int>(this.randomIndexOverride) : null,
                rotation = this.rotation,
                drawOffset = this.drawOffset,
                altitudeLayer = this.altitudeLayer,
                displayCustomPortraitInColonistBar = this.displayCustomPortraitInColonistBar
            };
        }
    }
}
