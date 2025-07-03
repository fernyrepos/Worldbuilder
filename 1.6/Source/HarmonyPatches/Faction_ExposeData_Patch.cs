using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.ExposeData))]
    public static class Faction_ExposeData_Patch
    {
        public static void Postfix(Faction __instance)
        {
            if (__instance.IsPlayer)
            {
                if ((Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.PostLoadInit)
                    && !string.IsNullOrEmpty(World_ExposeData_Patch.playerFactionName))
                {
                    __instance.Name = World_ExposeData_Patch.playerFactionName;
                }
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
                if (currentPreset?.factionNameOverrides != null &&
                    __instance.def != null &&
                    currentPreset.factionNameOverrides.TryGetValue(__instance.def, out var nameOverride) &&
                    !string.IsNullOrEmpty(nameOverride))
                {
                    __instance.Name = nameOverride;
                }
            }
        }
    }
}
