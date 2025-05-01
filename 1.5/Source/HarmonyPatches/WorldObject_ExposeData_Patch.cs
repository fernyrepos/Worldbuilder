using HarmonyLib;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.ExposeData))]
    public static class WorldObject_ExposeData_Patch
    {
        public static void Postfix(WorldObject __instance)
        {
            if (__instance.def == WorldbuilderDefOf.Worldbuilder_MapMarker)
            {
                string scribeLabel = "worldbuilder_markerData_" + __instance.ID;
                MarkerData data = null;
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    data = MarkerDataManager.GetData(__instance);
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
                        MarkerDataManager.LoadData(__instance, data);
                    }
                }
            }
        }
    }
}
