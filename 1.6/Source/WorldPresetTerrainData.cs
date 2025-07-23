using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using RimWorld;

namespace Worldbuilder
{
    public class WorldPresetTerrainData : IExposable
    {
        public Dictionary<int, Landmark> landmarks = new Dictionary<int, Landmark>();
        public byte[] tileBiome;
        public byte[] tileElevation;
        public byte[] tileHilliness;
        public byte[] tileTemperature;
        public byte[] tileRainfall;
        public byte[] tileSwampiness;
        public byte[] tilePollution;
        public byte[] tileFeature;
        public byte[] tileRoadOrigins;
        public byte[] tileRoadAdjacency;
        public byte[] tileRoadDef;
        public byte[] tileRiverOrigins;
        public byte[] tileRiverAdjacency;
        public byte[] tileRiverDef;
        public byte[] tileRiverDistances;
        public byte[] tileMutatorTiles;
        public byte[] tileMutatorDefs;
        public void ExposeData()
        {
            DataExposeUtility.LookByteArray(ref tileBiome, "tileBiome");
            DataExposeUtility.LookByteArray(ref tileElevation, "tileElevation");
            DataExposeUtility.LookByteArray(ref tileHilliness, "tileHilliness");
            DataExposeUtility.LookByteArray(ref tileTemperature, "tileTemperature");
            DataExposeUtility.LookByteArray(ref tileRainfall, "tileRainfall");
            DataExposeUtility.LookByteArray(ref tileSwampiness, "tileSwampiness");
            DataExposeUtility.LookByteArray(ref tilePollution, "tilePollution");
            DataExposeUtility.LookByteArray(ref tileFeature, "tileFeature");
            DataExposeUtility.LookByteArray(ref tileRoadOrigins, "tileRoadOrigins");
            DataExposeUtility.LookByteArray(ref tileRoadAdjacency, "tileRoadAdjacency");
            DataExposeUtility.LookByteArray(ref tileRoadDef, "tileRoadDef");
            DataExposeUtility.LookByteArray(ref tileRiverOrigins, "tileRiverOrigins");
            DataExposeUtility.LookByteArray(ref tileRiverAdjacency, "tileRiverAdjacency");
            DataExposeUtility.LookByteArray(ref tileRiverDef, "tileRiverDef");
            DataExposeUtility.LookByteArray(ref tileRiverDistances, "tileRiverDistances");
            DataExposeUtility.LookByteArray(ref tileMutatorTiles, "tileMutatorTiles");
            DataExposeUtility.LookByteArray(ref tileMutatorDefs, "tileMutatorDefs");
            Scribe_Collections.Look(ref landmarks, "landmarks", LookMode.Value, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                landmarks ??= new Dictionary<int, Landmark>();
            }
        }
    }
}
