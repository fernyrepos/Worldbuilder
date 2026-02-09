using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(StatsReportUtility))]
    public static class StatsReportUtility_Patch
    {
        [HarmonyPatch("StatsToDraw", new[] { typeof(Thing) })]
        [HarmonyPostfix]
        public static IEnumerable<StatDrawEntry> StatsToDraw_Thing_Postfix(
            IEnumerable<StatDrawEntry> __result, 
            Thing thing)
        {
            bool narrativeInserted = false;

            foreach (var entry in __result)
            {
                yield return entry;

                if (!narrativeInserted && entry.LabelCap == "Description".Translate())
                {
                    var narrativeEntry = CreateNarrativeEntry(thing);
                    if (narrativeEntry != null)
                    {
                        yield return narrativeEntry;
                        narrativeInserted = true;
                    }
                }
            }
        }

        [HarmonyPatch("StatsToDraw", new[] { typeof(WorldObject) })]
        [HarmonyPostfix]
        public static IEnumerable<StatDrawEntry> StatsToDraw_WorldObject_Postfix(
            IEnumerable<StatDrawEntry> __result, 
            WorldObject worldObject)
        {
            bool narrativeInserted = false;

            foreach (var entry in __result)
            {
                yield return entry;

                if (!narrativeInserted && entry.LabelCap == "Description".Translate())
                {
                    var narrativeEntry = CreateNarrativeEntry(worldObject);
                    if (narrativeEntry != null)
                    {
                        yield return narrativeEntry;
                        narrativeInserted = true;
                    }
                }
            }
        }

        private static StatDrawEntry CreateNarrativeEntry(Thing thing)
        {
            var customData = thing?.GetCustomizationData();
            if (customData == null || string.IsNullOrEmpty(customData.narrativeText))
                return null;

            return new StatDrawEntry(
                StatCategoryDefOf.BasicsImportant,
                "WB_Narrative".Translate(),
                "",
                customData.narrativeText,
                99998,
                null,
                null,
                forceUnfinalizedMode: false,
                overridesHideStats: true
            );
        }

        private static StatDrawEntry CreateNarrativeEntry(WorldObject worldObject)
        {
            string narrativeText = null;

            if (worldObject is Settlement settlement)
            {
                narrativeText = settlement.GetCustomizationData()?.narrativeText;
            }
            else if (worldObject is WorldObject_MapMarker mapMarker)
            {
                narrativeText = mapMarker.MarkerData?.narrativeText;
            }

            if (string.IsNullOrEmpty(narrativeText))
                return null;

            return new StatDrawEntry(
                StatCategoryDefOf.BasicsImportant,
                "WB_Narrative".Translate(),
                "",
                narrativeText,
                99998,
                null,
                null,
                forceUnfinalizedMode: false,
                overridesHideStats: true
            );
        }
    }
}
