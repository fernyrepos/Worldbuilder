using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(FactionDef), nameof(FactionDef.FactionIcon), MethodType.Getter)]
    public static class FactionDef_FactionIcon_Patch
    {
        public static void Postfix(FactionDef __instance, ref Texture2D __result)
        {
            if (World_ExposeData_Patch.individualFactionIcons.TryGetValue(__instance, out string path))
            {
                __result = ContentFinder<Texture2D>.Get(path);
            }
            else if (World_ExposeData_Patch.individualFactionIdeoIcons.TryGetValue(__instance, out IdeoIconDef iconDef))
            {
                __result = iconDef.Icon;
            }
        }
    }
}
