using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(Faction), nameof(Faction.Name), MethodType.Getter)]
    public static class Faction_Name_Patch
    {
        public static void Postfix(Faction __instance, ref string __result)
        {
            if (__instance.IsPlayer && !string.IsNullOrEmpty(World_ExposeData_Patch.playerFactionName))
            {
                __result = World_ExposeData_Patch.playerFactionName;
            }
            else
            {
                if (World_ExposeData_Patch.individualFactionNames != null &&World_ExposeData_Patch.individualFactionNames.TryGetValue(__instance.def, out var individualName) && !individualName.NullOrEmpty())
                {
                    __result = individualName;
                }
                else
                {
                    var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
                    if (currentPreset?.factionNameOverrides != null &&
                        currentPreset.factionNameOverrides.TryGetValue(__instance.def, out var nameOverride) && !string.IsNullOrEmpty(nameOverride))
                    {
                        __result = nameOverride;
                    }
                }
            }
        }
    }
}
