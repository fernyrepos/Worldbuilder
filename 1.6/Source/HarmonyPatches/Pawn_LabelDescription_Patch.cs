using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Pawn))]
    public static class Pawn_LabelDescription_Patch
    {
        [HarmonyPatch(nameof(Pawn.LabelNoCount), MethodType.Getter)]
        [HarmonyPostfix]
        public static void LabelNoCount_Postfix(Pawn __instance, ref string __result)
        {
            if (!__instance.RaceProps.Humanlike)
            {
                var data = __instance.GetCustomizationData();
                if (data != null && !string.IsNullOrEmpty(data.labelOverride))
                {
                    __result = data.labelOverride;
                }
            }
        }

        [HarmonyPatch(nameof(Pawn.DescriptionFlavor), MethodType.Getter)]
        [HarmonyPostfix]
        public static void DescriptionFlavor_Postfix(Pawn __instance, ref string __result)
        {
            if (!__instance.RaceProps.Humanlike)
            {
                var data = __instance.GetCustomizationData();
                if (data != null && !string.IsNullOrEmpty(data.descriptionOverride))
                {
                    __result = data.descriptionOverride;
                }
            }
        }
    }
}