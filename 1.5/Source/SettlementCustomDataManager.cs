using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    public static class SettlementCustomDataManager
    {
        private static Dictionary<int, SettlementCustomData> dataStore = new Dictionary<int, SettlementCustomData>();
        public static SettlementCustomData GetData(Settlement settlement)
        {
            if (settlement == null) return null;
            dataStore.TryGetValue(settlement.ID, out var data);
            return data;
        }
        public static SettlementCustomData GetOrCreateData(Settlement settlement)
        {
            if (settlement == null) return null;
            if (!dataStore.TryGetValue(settlement.ID, out var data))
            {
                data = new SettlementCustomData();
                dataStore[settlement.ID] = data;
            }
            return data;
        }
        public static void CleanupOrphanedData()
        {
            if (dataStore == null || dataStore.Count == 0) return;

            var existingSettlementIDs = Find.WorldObjects.Settlements.Select(s => s.ID).ToHashSet();
            dataStore = dataStore
                .Where(kvp => existingSettlementIDs.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        public static void LoadData(Settlement settlement, SettlementCustomData loadedData)
        {
            if (settlement != null && loadedData != null)
            {
                dataStore[settlement.ID] = loadedData;
            }
        }
    }
}
