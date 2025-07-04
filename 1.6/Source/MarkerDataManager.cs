using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{

    public static class MarkerDataManager
    {
        private static Dictionary<int, MarkerData> dataStore = new Dictionary<int, MarkerData>();

        public static MarkerData GetData(WorldObject marker)
        {
            if (marker == null) return null;
            if (!dataStore.TryGetValue(marker.ID, out var data))
            {
                dataStore[marker.ID] = data = new MarkerData();
            }
            return data;
        }
        
        public static void RemoveData(WorldObject marker)
        {
            if (marker != null)
            {
                dataStore.Remove(marker.ID);
            }
        }
        public static void CleanupOrphanedData(List<WorldObject> allMarkers)
        {
            if (dataStore == null || dataStore.Count == 0) return;

            var existingMarkerIDs = allMarkers.Select(m => m.ID).ToHashSet();
            dataStore = dataStore
                .Where(kvp => existingMarkerIDs.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        public static void LoadData(WorldObject marker, MarkerData loadedData)
        {
            if (marker != null && loadedData != null)
            {
                dataStore[marker.ID] = loadedData;
            }
        }
        public static void ClearData()
        {
            dataStore.Clear();
        }

        public static void SetData(WorldObject_MapMarker marker, MarkerData markerData)
        {
            dataStore[marker.ID] = markerData;
        }
    }
}
