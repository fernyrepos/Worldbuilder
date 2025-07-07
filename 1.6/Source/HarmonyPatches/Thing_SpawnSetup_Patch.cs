using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(Thing), "SpawnSetup")]
    public static class Thing_SpawnSetup_Patch
    {
        public static void Postfix(Thing __instance)
        {
            __instance.UpdateGraphic();
        }

        public static void UpdateGraphic(this Thing __instance)
        {
            if (__instance is not Pawn)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    LongEventHandler.toExecuteWhenFinished.Add(delegate
                    {
                        CustomizationData customizationData = __instance.GetCustomizationData();
                        if (customizationData != null)
                        {
                            customizationData.SetGraphic(__instance);
                        }
                        else
                        {
                            __instance.LogMessage($"No customization data found. Skipping graphic update.");
                        }
                    });
                });
            }
        }
    }
}
