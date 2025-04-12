using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Thing), "SpawnSetup")]
    public static class Thing_SpawnSetup_Patch
    {
        public static void Postfix(Thing __instance)
        {
            __instance.UpdateGraphic();
        }

        public static void UpdateGraphic(this Thing __instance)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                CustomizationData customizationData = __instance.GetCustomizationData();
                if (customizationData != null)
                {
                    customizationData.SetGraphic(__instance);
                }
            });
        }
    }
}