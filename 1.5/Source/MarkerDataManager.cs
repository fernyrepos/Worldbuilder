using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    public class MarkerData : IExposable
    {
        public string name;
        public string description;
        public string narrativeText;
        public string iconDefName;
        public Color color = Color.white;

        public IdeoIconDef IconDef => string.IsNullOrEmpty(iconDefName) ? null : DefDatabase<IdeoIconDef>.GetNamed(iconDefName, false);

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref narrativeText, "narrativeText");
            Scribe_Values.Look(ref iconDefName, "iconDefName");
            Scribe_Values.Look(ref color, "color", Color.white);
        }

        public MarkerData Copy()
        {
            return new MarkerData
            {
                name = this.name,
                description = this.description,
                narrativeText = this.narrativeText,
                iconDefName = this.iconDefName,
                color = this.color
            };
        }
    }
    public static class MarkerDataManager
    {
        private static Dictionary<int, MarkerData> dataStore = new Dictionary<int, MarkerData>();

        public static MarkerData GetData(WorldObject marker)
        {
            if (marker == null) return null;
            dataStore.TryGetValue(marker.ID, out var data);
            return data;
        }

        public static MarkerData GetOrCreateData(WorldObject marker)
        {
            if (marker == null) return null;
            if (!dataStore.TryGetValue(marker.ID, out var data))
            {
                data = new MarkerData();
                dataStore[marker.ID] = data;
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
    }
}