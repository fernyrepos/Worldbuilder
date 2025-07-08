using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.GetDescription))]
    public static class WorldObject_GetDescription_Patch
    {
        public static void Postfix(WorldObject __instance, ref string __result)
        {
            if (__instance is Settlement settlement)
            {
                var data = settlement.GetCustomizationData();
                if (data != null && !data.description.NullOrEmpty())
                {
                    __result = data.description;
                }
            }
        }
    }
}
