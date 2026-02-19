using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public static class ModCompatibilityHelper
    {
        public const string MyLittlePlanetPackageId = "Oblitus.MyLittlePlanet";
        public const string WorldTechLevelPackageId = "m00nl1ght.WorldTechLevel";
        public const string VFEInsectoids2PackageId = "OskarPotocki.VFE.Insectoid2";
        private static FieldInfo wtlUnrestrictedField;
        private static object wtlSettingsInstance;
        private static FieldInfo wtlUnrestrictedValueField;
        private static FieldInfo vfe2InstanceField;
        private static FieldInfo vfe2InsectTerritoryScaleField;
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

        public static void ApplyWTLChanges(Page_CreateWorldParams page)
        {
            if (!ModsConfig.IsActive(WorldTechLevelPackageId)) return;

            var patchType = AccessTools.TypeByName("WorldTechLevel.Patches.Patch_Page_CreateWorldParams");
            if (patchType == null) return;

            var applyChangesMethod = AccessTools.Method(patchType, "ApplyChanges", new Type[] { typeof(List<FactionDef>), typeof(float).MakeByRefType() });
            if (applyChangesMethod == null) return;

            object[] args = new object[] { page.factions, page.pollution };
            applyChangesMethod.Invoke(null, args);
            page.pollution = (float)args[1];
        }

        private static bool PrepareVFE2Reflection()
        {
            if (vfe2InstanceField != null) return true;
            var vfe2GameComponentType = AccessTools.TypeByName("VFEInsectoids.GameComponent_Insectoids");
            if (vfe2GameComponentType == null)
            {
                Log.Error("Worldbuilder: Could not find type VFEInsectoids.GameComponent_Insectoids");
                return false;
            }

            vfe2InstanceField = AccessTools.Field(vfe2GameComponentType, "Instance");
            if (vfe2InstanceField == null)
            {
                Log.Error("Worldbuilder: Could not find field Instance on VFEInsectoids.GameComponent_Insectoids");
                return false;
            }

            vfe2InsectTerritoryScaleField = AccessTools.Field(vfe2GameComponentType, "insectTerritoryScale");
            if (vfe2InsectTerritoryScaleField == null)
            {
                Log.Error("Worldbuilder: Could not find field insectTerritoryScale on VFEInsectoids.GameComponent_Insectoids");
                return false;
            }

            return true;
        }

        public static void TryAddVFE2InsectTerritoryScaleSlider(float x, float y, float labelWidth, float sliderWidth, float height)
        {
            if (!PrepareVFE2Reflection()) return;

            var instance = vfe2InstanceField.GetValue(null);
            if (instance == null)
            {
                Log.Error("Worldbuilder: VFEInsectoids GameComponent_Insectoids.Instance is null");
                return;
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, labelWidth, height), "VFEI_InsectTerritoryScale".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            var currentScale = (float)vfe2InsectTerritoryScaleField.GetValue(instance);
            var newScale = Widgets.HorizontalSlider(new Rect(x + labelWidth + 5f, y, sliderWidth, height), currentScale, 0f, 2f, middleAlignment: true,
                currentScale.ToStringPercent(), null, null, 0.05f);
            vfe2InsectTerritoryScaleField.SetValue(instance, newScale);
        }
    }
}
