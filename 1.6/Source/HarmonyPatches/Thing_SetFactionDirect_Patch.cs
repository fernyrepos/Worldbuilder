using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.SetFactionDirect))]
    public static class Thing_SetFactionDirect_Patch
    {
        public static void Postfix(Thing __instance, Faction newFaction)
        {
            if (!__instance.Spawned && newFaction != null && newFaction == Faction.OfPlayerSilentFail)
            {
                CustomizationDataCollections.TryAssignPlayerDefault(null, __instance);
            }
        }
    }
}
