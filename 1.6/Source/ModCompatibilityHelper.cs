using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public static class ModCompatibilityHelper
    {
        public const string MyLittlePlanetPackageId = "Oblitus.MyLittlePlanet";
        public const string WorldTechLevelPackageId = "m00nl1ght.WorldTechLevel";
        private static FieldInfo wtlUnrestrictedField;
        private static object wtlSettingsInstance;
        private static FieldInfo wtlUnrestrictedValueField;
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

        private static bool PrepareWTLUnrestrictedReflection()
        {
            if (wtlUnrestrictedField != null) return true;
            if (!ModsConfig.IsActive(WorldTechLevelPackageId)) return false;
            var wtlModType = AccessTools.TypeByName("WorldTechLevel.WorldTechLevel");
            if (wtlModType == null)
            {
                Log.Error("Worldbuilder: Could not find type WorldTechLevel.WorldTechLevel");
                return false;
            }
            var settingsField = AccessTools.Field(wtlModType, "Settings");
            if (settingsField == null)
            {
                Log.Error("Worldbuilder: Could not find field Settings on WorldTechLevel.WorldTechLevel");
                return false;
            }
            wtlSettingsInstance = settingsField.GetValue(null);
            if (wtlSettingsInstance == null)
            {
                Log.Error("Worldbuilder: Settings field on WorldTechLevel.WorldTechLevel is null");
                return false;
            }
            wtlUnrestrictedField = AccessTools.Field(wtlSettingsInstance.GetType(), "AlwaysDefaultToUnrestricted");
            if (wtlUnrestrictedField == null)
            {
                Log.Error("Worldbuilder: Could not find field AlwaysDefaultToUnrestricted on WorldTechLevel settings");
                return false;
            }
            return true;
        }

        public static bool TryGetWTLUnrestricted(out bool isUnrestricted)
        {
            isUnrestricted = true;
            if (!PrepareWTLUnrestrictedReflection()) return false;

            var entryObject = wtlUnrestrictedField.GetValue(wtlSettingsInstance);
            if (entryObject == null)
            {
                Log.Error("Worldbuilder: AlwaysDefaultToUnrestricted field value is null.");
                return false;
            }

            if (wtlUnrestrictedValueField == null)
            {
                wtlUnrestrictedValueField = AccessTools.Field(entryObject.GetType(), "Value");
            }

            if (wtlUnrestrictedValueField == null)
            {
                Log.Error("Worldbuilder: Could not find 'Value' field on the Entry<bool> object.");
                return false;
            }

            isUnrestricted = (bool)wtlUnrestrictedValueField.GetValue(entryObject);
            return true;
        }

        public static bool TrySetWTLUnrestricted(bool isUnrestricted)
        {
            if (!PrepareWTLUnrestrictedReflection()) return false;

            var entryObject = wtlUnrestrictedField.GetValue(wtlSettingsInstance);
            if (entryObject == null)
            {
                Log.Error("Worldbuilder: AlwaysDefaultToUnrestricted field value is null.");
                return false;
            }

            if (wtlUnrestrictedValueField == null)
            {
                wtlUnrestrictedValueField = AccessTools.Field(entryObject.GetType(), "Value");
            }

            if (wtlUnrestrictedValueField == null)
            {
                Log.Error("Worldbuilder: Could not find 'Value' field on the Entry<bool> object.");
                return false;
            }

            wtlUnrestrictedValueField.SetValue(entryObject, isUnrestricted);
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
            var currentTechLevelField = AccessTools.PropertySetter(wtlModType, "Current"); currentTechLevelField.Invoke(null, new object[] { techLevel });

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
