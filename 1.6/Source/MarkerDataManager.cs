using RimWorld.Planet;
using System.Collections.Generic;

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
