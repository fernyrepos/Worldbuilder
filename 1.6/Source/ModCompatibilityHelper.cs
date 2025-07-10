using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public static class ModCompatibilityHelper
    {
        public const string MyLittlePlanetPackageId = "Oblitus.MyLittlePlanet";
        public const string WorldTechLevelPackageId = "m00nl1ght.WorldTechLevel";
        public static bool TryGetMLPSubcount(out int subcount)
        {
            subcount = 10;
            if (!ModsConfig.IsActive(MyLittlePlanetPackageId)) return false;

            var rulesOverriderType = AccessTools.TypeByName("WorldGenRules.WorldGenRules");
            var subcountField = AccessTools.Field(rulesOverriderType, "subcount");
            subcount = (int)subcountField.GetValue(null);
            return true;
        }

        public static bool TrySetMLPSubcount(int subcount)
        {
            if (!ModsConfig.IsActive(MyLittlePlanetPackageId)) return false;
            if (subcount < 6 || subcount > 10) return false;

            var rulesOverriderType = AccessTools.TypeByName("WorldGenRules.WorldGenRules");
            var subcountField = AccessTools.Field(rulesOverriderType, "subcount");
            subcountField.SetValue(null, subcount);
            PlanetLayerSettingsDefOf.Surface.settings.subdivisions = subcount;
            return true;
        }

        public static bool TryGetWTL(out TechLevel techLevel)
        {
            techLevel = TechLevel.Undefined;
            if (!ModsConfig.IsActive(WorldTechLevelPackageId)) return false;

            var wtlModType = AccessTools.TypeByName("WorldTechLevel.WorldTechLevel");
            var currentTechLevelField = AccessTools.PropertyGetter(wtlModType, "Current");
            techLevel = (TechLevel)currentTechLevelField.Invoke(null, null);
            return true;
        }

        public static bool TrySetWTL(TechLevel techLevel)
        {
            if (!ModsConfig.IsActive(WorldTechLevelPackageId)) return false;

            var wtlModType = AccessTools.TypeByName("WorldTechLevel.WorldTechLevel");
            var currentTechLevelField = AccessTools.PropertySetter(wtlModType, "Current"); currentTechLevelField.Invoke(null, new object[] { techLevel } );
            
            if (Current.Game != null)
            {
                var gameCompType = AccessTools.TypeByName("WorldTechLevel.GameComponent_TechLevel");
                if (gameCompType != null)
                {
                    var gameCompInstance = Current.Game.GetComponent(gameCompType);
                    var worldTechLevelField = AccessTools.PropertySetter(gameCompType, "WorldTechLevel");
                    worldTechLevelField.Invoke(gameCompInstance, new object[] { techLevel });
                }
            }
            return true;
        }
    }
}
