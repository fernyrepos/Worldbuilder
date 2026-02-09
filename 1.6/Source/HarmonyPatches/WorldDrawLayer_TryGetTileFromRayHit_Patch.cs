using HarmonyLib;
using RimWorld.Planet;
using System;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldDrawLayer), nameof(WorldDrawLayer.TryGetTileFromRayHit))]
    public static class WorldDrawLayer_TryGetTileFromRayHit_Patch
    {
        public static Exception Finalizer(Exception __exception, ref PlanetTile id, ref bool __result)
        {
            if (__exception is ArgumentOutOfRangeException)
            {
                id = PlanetTile.Invalid;
                __result = false;
                return null;
            }
            return __exception;
        }
    }
}
