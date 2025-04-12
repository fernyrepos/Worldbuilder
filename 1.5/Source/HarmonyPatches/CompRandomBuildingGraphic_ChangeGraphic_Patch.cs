using HarmonyLib;
using VanillaFurnitureExpanded;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(CompRandomBuildingGraphic), "ChangeGraphic")]
    public static class CompRandomBuildingGraphic_ChangeGraphic_Patch
    {
        public static bool Prefix(CompRandomBuildingGraphic __instance)
        {
            if (__instance.parent.Customizable())
            {
                return false;
            }
            return true;
        }
    }
}