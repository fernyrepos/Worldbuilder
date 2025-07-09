using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Label), MethodType.Getter)]
    public static class Designator_Build_Label_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref string __result)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                CustomizationData data = def.GetCustomizationDataPlayer();
                if (data != null)
                {
                    if (!string.IsNullOrEmpty(data.labelOverride))
                    {
                        string newLabel = data.labelOverride;
                        if (__instance.sourcePrecept != null)
                        {
                            newLabel = __instance.sourcePrecept.TransformThingLabel(newLabel);
                        }
                        if (def != null && !__instance.writeStuff && def.MadeFromStuff)
                        {
                            newLabel += "...";
                        }
                        __result = newLabel;
                    }
                }
            }
        }
    }
}
