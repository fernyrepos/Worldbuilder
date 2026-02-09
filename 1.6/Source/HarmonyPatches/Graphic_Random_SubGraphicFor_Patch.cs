using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Graphic_Random), nameof(Graphic_Random.SubGraphicFor))]
    public static class Graphic_Random_SubGraphicFor_Patch
    {
        public static void Postfix(Graphic_Random __instance, Thing thing, ref Graphic __result)
        {
            if (thing == null)
            {
                return;
            }

            var customizationData = thing.GetCustomizationData();
            if (customizationData == null || customizationData.randomIndexOverride == null)
            {
                return;
            }

            if (customizationData.randomIndexOverride.TryGetValue(customizationData.RandomIndexKey, out int overrideIndex))
            {
                __result = __instance.subGraphics[overrideIndex];
            }
        }
    }
}
