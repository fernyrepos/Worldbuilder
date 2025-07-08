using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Desc), MethodType.Getter)]
    public static class Designator_Build_Desc_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref string __result)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                CustomizationData data = def.GetCustomizationDataPlayer();
                if (data != null)
                {
                    if (!string.IsNullOrEmpty(data.descriptionOverride))
                    {
                        __result = data.descriptionOverride;
                    }
                }
            }
        }
    }
}
