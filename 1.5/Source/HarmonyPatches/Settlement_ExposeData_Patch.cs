using HarmonyLib;
using RimWorld;
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

    [HarmonyPatch(typeof(Settlement), nameof(Settlement.ExposeData))]
    public static class Settlement_ExposeData_Patch
    {
        public static void Postfix(Settlement __instance)
        {
            string scribeLabel = "worldbuilder_customData_" + __instance.ID;
            SettlementCustomData data = null;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                data = SettlementCustomDataManager.GetData(__instance);
                if (data != null)
                {
                    Scribe_Deep.Look(ref data, scribeLabel);
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Deep.Look(ref data, scribeLabel);
                if (data != null)
                {
                    SettlementCustomDataManager.LoadData(__instance, data);
                }
            }
        }
    }
    [HarmonyPatch(typeof(Game), "ExposeData")]
    public static class Game_ExposeData_Cleanup_Patch
    {
        public static void Postfix()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                SettlementCustomDataManager.CleanupOrphanedData();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
            }
        }
    }
}