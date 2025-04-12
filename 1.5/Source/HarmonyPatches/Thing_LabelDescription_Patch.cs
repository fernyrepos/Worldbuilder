using HarmonyLib;
using Verse;

namespace Worldbuilder
{

    [HarmonyPatch(typeof(Thing), nameof(Thing.DescriptionFlavor), MethodType.Getter)]
    public static class Thing_DescriptionFlavor_Patch
    {
        static void Postfix(Thing __instance, ref string __result)
        {
            if (__instance.GetCustomizationData() is CustomizationData customizationData && !string.IsNullOrEmpty(customizationData.descriptionOverride))
            {
                __result = customizationData.descriptionOverride;
            }
        }
    }
}