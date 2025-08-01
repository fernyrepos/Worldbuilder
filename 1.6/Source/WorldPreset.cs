using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

namespace Worldbuilder
{
    public class WorldPreset : IExposable
    {
        public string name;
        public string description;
        public bool saveFactions;
        public bool saveIdeologies;
        public bool saveTerrain;
        public bool saveBases;
        public bool saveMapMarkers;
        public bool saveWorldFeatures;
        public bool saveStorykeeperEntries;
        public bool saveWorldTechLevel;
        public List<ScenPart> scenParts;
        public List<string> scenPartDefs;
        public int myLittlePlanetSubcount;
        public TechLevel worldTechLevel = TechLevel.Undefined;
        public Dictionary<string, CustomizationData> customizationDefaults;
        public Dictionary<string, SettlementCustomData> factionSettlementCustomizationDefaults;
        public Dictionary<string, string> factionNameOverrides;
        public Dictionary<string, string> factionDescriptionOverrides;
        public List<Story> presetStories = new List<Story>();
        public Dictionary<string, List<string>> savedIdeoFactionMapping;
        public WorldInfo worldInfo;
        public List<SettlementSaveData> savedSettlementsData;
        public List<MapMarkerSaveData> savedMapMarkersData;
        public List<MapTextSaveData> savedWorldFeaturesData;
        public List<string> savedFactionDefs;
        public Dictionary<string, string> pathOverrides;
        private string _presetFolder;
        public string PresetFolder
        {
            get
            {
                if (!string.IsNullOrEmpty(_presetFolder))
                {
                    return _presetFolder;
                }
                return Path.Combine(GenFilePaths.FolderUnderSaveData("Worldbuilder"), name);
            }
            set
            {
                _presetFolder = value;
            }
        }
        public const string CustomImagesFolderName = "CustomImages";
        public const string CustomIdeosFolderName = "CustomIdeos";
        public const string ThumbnailFileName = "Thumbnail.png";
        public const string FlavorFileName = "Flavor.png";
        public const string PresetFileName = "Preset.xml";
        public const string TerrainDataFileName = "TerrainData.xml";
        public string CustomImagesPath
        {
            get
            {
                if (pathOverrides != null && pathOverrides.TryGetValue(nameof(CustomImagesPath), out var overridePath))
                {
                    return overridePath;
                }
                return Path.Combine(PresetFolder, CustomImagesFolderName);
            }
        }
        public string CustomIdeosPath
        {
            get
            {
                if (pathOverrides != null && pathOverrides.TryGetValue(nameof(CustomIdeosPath), out var overridePath))
                {
                    return overridePath;
                }
                return Path.Combine(PresetFolder, CustomIdeosFolderName);
            }
        }
        public string ThumbnailPath
        {
            get
            {
                if (pathOverrides != null && pathOverrides.TryGetValue(nameof(ThumbnailPath), out var overridePath))
                {
                    return overridePath;
                }
                return Path.Combine(PresetFolder, ThumbnailFileName);
            }
        }
        public string FlavorImagePath
        {
            get
            {
                if (pathOverrides != null && pathOverrides.TryGetValue(nameof(FlavorImagePath), out var overridePath))
                {
                    return overridePath;
                }
                return Path.Combine(PresetFolder, FlavorFileName);
            }
        }
        public string PresetFilePath
        {
            get
            {
                if (pathOverrides != null && pathOverrides.TryGetValue(nameof(PresetFilePath), out var overridePath))
                {
                    return overridePath;
                }
                return Path.Combine(PresetFolder, PresetFileName);
            }
        }
        public string TerrainDataFilePath
        {
            get
            {
                if (pathOverrides != null && pathOverrides.TryGetValue(nameof(TerrainDataFilePath), out var overridePath))
                {
                    return overridePath;
                }
                return Path.Combine(PresetFolder, TerrainDataFileName);
            }
        }
        public string planetType;
        public int difficulty;

        public List<string> biomes;
        public List<string> landmarks;
        public List<string> features;

        private WorldPresetTerrainData _terrainData;
        public WorldPresetTerrainData TerrainData
        {
            get
            {
                if (_terrainData == null)
                {
                    _terrainData = WorldPresetManager.LoadTerrainData(this);
                    if (_terrainData == null)
                    {
                        _terrainData = new WorldPresetTerrainData();
                        WorldPresetManager.SaveTerrainData(this, _terrainData);
                    }
                }
                return _terrainData;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref planetType, "planetType");
            Scribe_Values.Look(ref difficulty, "difficulty", defaultValue: 2);
            Scribe_Collections.Look(ref biomes, "biomes", LookMode.Value);
            Scribe_Collections.Look(ref landmarks, "landmarks", LookMode.Value);
            Scribe_Collections.Look(ref features, "features", LookMode.Value);
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref saveFactions, "saveFactions", defaultValue: false);
            Scribe_Values.Look(ref saveIdeologies, "saveIdeologies", defaultValue: false);
            Scribe_Values.Look(ref saveTerrain, "saveTerrain", defaultValue: false);
            Scribe_Values.Look(ref saveBases, "saveBases", defaultValue: false);
            Scribe_Values.Look(ref saveMapMarkers, "saveMapMarkers", defaultValue: false);
            Scribe_Values.Look(ref saveWorldFeatures, "saveMapText", defaultValue: false);
            Scribe_Values.Look(ref saveStorykeeperEntries, "saveStorykeeperEntries", defaultValue: false);
            Scribe_Values.Look(ref saveWorldTechLevel, "saveWorldTechLevel", defaultValue: false);
            Scribe_Collections.Look(ref scenParts, "scenParts", LookMode.Deep);
            Scribe_Collections.Look(ref scenPartDefs, "scenPartDefs", LookMode.Value);
            Scribe_Values.Look(ref myLittlePlanetSubcount, "myLittlePlanetSubcount", defaultValue: 10);
            Scribe_Values.Look(ref worldTechLevel, "worldTechLevel", defaultValue: TechLevel.Undefined);
            Scribe_Collections.Look(ref customizationDefaults, "customizationDefaults", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref factionSettlementCustomizationDefaults, "factionSettlementCustomizationDefaults", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref factionNameOverrides, "factionNameOverrides", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref factionDescriptionOverrides, "factionDescriptionOverrides", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref presetStories, "presetStories", LookMode.Deep);
            Scribe_Collections.Look(ref savedFactionDefs, "savedFactionDefs", LookMode.Value);
            Scribe_Collections.Look(ref savedSettlementsData, "savedSettlementsData", LookMode.Deep);
            Scribe_Collections.Look(ref savedMapMarkersData, "savedMapMarkersData", LookMode.Deep);
            Scribe_Collections.Look(ref savedWorldFeaturesData, "savedMapTextFeaturesData", LookMode.Deep);
            BackCompatibility_PostExposeData_Patch.shouldPrevent = true;
            Log.logDisablers = 1;
            Scribe_Deep.Look(ref worldInfo, "worldInfo");
            Log.logDisablers = 0;
            BackCompatibility_PostExposeData_Patch.shouldPrevent = false;
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                customizationDefaults ??= new Dictionary<string, CustomizationData>();
                factionSettlementCustomizationDefaults ??= new Dictionary<string, SettlementCustomData>();
                factionDescriptionOverrides ??= new Dictionary<string, string>();
                factionNameOverrides ??= new Dictionary<string, string>();
                presetStories ??= new List<Story>();
                savedFactionDefs ??= new List<string>();
                savedSettlementsData ??= new List<SettlementSaveData>();
                savedMapMarkersData ??= new List<MapMarkerSaveData>();
                savedWorldFeaturesData ??= new List<MapTextSaveData>();
                scenParts ??= new List<ScenPart>();
                scenPartDefs ??= new List<string>();
            }
        }
        public void AddPathOverridesFrom(string folderPath)
        {
            pathOverrides ??= new Dictionary<string, string>();

            string thumbnailPath = Path.Combine(folderPath, ThumbnailFileName);
            if (File.Exists(thumbnailPath)) pathOverrides[nameof(ThumbnailPath)] = thumbnailPath;

            string flavorImagePath = Path.Combine(folderPath, FlavorFileName);
            if (File.Exists(flavorImagePath)) pathOverrides[nameof(FlavorImagePath)] = flavorImagePath;

            string presetFilePath = Path.Combine(folderPath, PresetFileName);
            if (File.Exists(presetFilePath)) pathOverrides[nameof(PresetFilePath)] = presetFilePath;

            string terrainDataFilePath = Path.Combine(folderPath, TerrainDataFileName);
            if (File.Exists(terrainDataFilePath)) pathOverrides[nameof(TerrainDataFilePath)] = terrainDataFilePath;

            string customImagesPath = Path.Combine(folderPath, CustomImagesFolderName);
            if (Directory.Exists(customImagesPath)) pathOverrides[nameof(CustomImagesPath)] = customImagesPath;

            string customIdeosPath = Path.Combine(folderPath, CustomIdeosFolderName);
            if (Directory.Exists(customIdeosPath)) pathOverrides[nameof(CustomIdeosPath)] = customIdeosPath;
        }
    }
}
