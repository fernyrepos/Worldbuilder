using Verse;
using RimWorld;
using Worldbuilder;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;

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
        public OverallRainfall rainfall = OverallRainfall.Normal;
        public OverallTemperature temperature = OverallTemperature.Normal;
        public int myLittlePlanetSubcount;
        public TechLevel worldTechLevel = TechLevel.Undefined;
        public OverallPopulation population;

        public Dictionary<ThingDef, CustomizationData> customizationDefaults;
        public Dictionary<FactionDef, SettlementCustomData> factionSettlementCustomizationDefaults;
        public Dictionary<FactionDef, string> factionNameOverrides;
        public List<Story> presetStories = new List<Story>();

        public List<FactionDef> savedFactionDefs;
        public Dictionary<string, List<string>> savedIdeoFactionMapping;
        public string savedPlanetName;
        public float savedPlanetCoverage = -1f;
        public string savedSeedString;
        public int savedPersistentRandomValue;
        public float savedPollution = -1f;

        public Dictionary<int, float> savedTilePollution;

        public List<SettlementSaveData> savedSettlementsData;
        public List<MapMarkerSaveData> savedMapMarkersData;
        public List<MapTextSaveData> savedWorldFeaturesData;
        public WorldGrid savedWorldGrid;

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref description, "description");

            Scribe_Values.Look(ref saveFactions, "saveFactions", defaultValue: false);
            Scribe_Values.Look(ref saveIdeologies, "saveIdeologies", defaultValue: false);
            Scribe_Values.Look(ref saveTerrain, "saveTerrain", defaultValue: false);
            Scribe_Values.Look(ref saveBases, "saveBases", defaultValue: false);
            Scribe_Values.Look(ref saveMapMarkers, "saveMapMarkers", defaultValue: false);
            Scribe_Values.Look(ref saveWorldFeatures, "saveMapText", defaultValue: false);
            Scribe_Values.Look(ref saveStorykeeperEntries, "saveStorykeeperEntries", defaultValue: false);
            Scribe_Values.Look(ref rainfall, "rainfall", defaultValue: OverallRainfall.Normal);
            Scribe_Values.Look(ref temperature, "temperature", defaultValue: OverallTemperature.Normal);

            Scribe_Values.Look(ref myLittlePlanetSubcount, "myLittlePlanetSubcount", defaultValue: 10);
            Scribe_Values.Look(ref worldTechLevel, "worldTechLevel", defaultValue: TechLevel.Undefined);
            Scribe_Values.Look(ref population, "population", defaultValue: OverallPopulation.Normal);

            Scribe_Collections.Look(ref customizationDefaults, "customizationDefaults", LookMode.Def, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && customizationDefaults == null)
            {
                customizationDefaults = new Dictionary<ThingDef, CustomizationData>();
            }
            Scribe_Collections.Look(ref factionSettlementCustomizationDefaults, "factionSettlementCustomizationDefaults", LookMode.Def, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && factionSettlementCustomizationDefaults == null)
            {
                factionSettlementCustomizationDefaults = new Dictionary<FactionDef, SettlementCustomData>();
            }

            Scribe_Collections.Look(ref factionNameOverrides, "factionNameOverrides", LookMode.Def, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && factionNameOverrides == null)
            {
                factionNameOverrides = new Dictionary<FactionDef, string>();
            }
            Scribe_Collections.Look(ref presetStories, "presetStories", LookMode.Deep, new object[0]);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && presetStories == null)
            {
                presetStories = new List<Story>();
            }

            Scribe_Collections.Look(ref savedFactionDefs, "savedFactionDefs", LookMode.Def);
            Scribe_Values.Look(ref savedPlanetName, "savedPlanetName");
            Scribe_Values.Look(ref savedPlanetCoverage, "savedPlanetCoverage", -1f);
            Scribe_Values.Look(ref savedSeedString, "savedSeedString");
            Scribe_Values.Look(ref savedPersistentRandomValue, "savedPersistentRandomValue");
            Scribe_Values.Look(ref savedPollution, "savedPollution", -1f);

            Scribe_Collections.Look(ref savedTilePollution, "savedTilePollution", LookMode.Value, LookMode.Value);

            Scribe_Collections.Look(ref savedSettlementsData, "savedSettlementsData", LookMode.Deep);
            Scribe_Collections.Look(ref savedMapMarkersData, "savedMapMarkersData", LookMode.Deep);
            Scribe_Collections.Look(ref savedWorldFeaturesData, "savedMapTextFeaturesData", LookMode.Deep);
            Scribe_Deep.Look(ref savedWorldGrid, "savedWorldGrid");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                savedFactionDefs ??= new List<FactionDef>();
                savedTilePollution ??= new Dictionary<int, float>();
                savedSettlementsData ??= new List<SettlementSaveData>();
                savedMapMarkersData ??= new List<MapMarkerSaveData>();
                savedWorldFeaturesData ??= new List<MapTextSaveData>();
            }
        }
    }
}


public class SettlementSaveData : IExposable
{
    public int tileID = -1;
    public string factionDefName;
    public string name;
    public SettlementCustomData customData;

    public void ExposeData()
    {
        Scribe_Values.Look(ref tileID, "tileID", -1);
        Scribe_Values.Look(ref factionDefName, "factionDefName");
        Scribe_Values.Look(ref name, "name");
        Scribe_Deep.Look(ref customData, "customData");
    }
}

public class MapMarkerSaveData : IExposable
{
    public int tileID = -1;
    public MarkerData markerData;

    public void ExposeData()
    {
        Scribe_Values.Look(ref tileID, "tileID", -1);
        Scribe_Deep.Look(ref markerData, "markerData");
    }
}

public class MapTextSaveData : IExposable
{
    public int tileID = -1;
    public string labelText;

    public void ExposeData()
    {
        Scribe_Values.Look(ref tileID, "tileID", -1);
        Scribe_Values.Look(ref labelText, "labelText");
    }
}
