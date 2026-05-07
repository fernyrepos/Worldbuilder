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
        public const string FactionTerritoriesPackageId = "jaeger972.factionterritories";
        private static FieldInfo wtlUnrestrictedField;
        private static object wtlSettingsInstance;
        private static FieldInfo wtlUnrestrictedValueField;
        private static FieldInfo vfe2InstanceField;
        private static FieldInfo vfe2InsectTerritoryScaleField;
        private static Type factionTerritoriesSettingsType;
        private static object factionTerritoriesSettingsInstance;
        private static MethodInfo setOverrideColorMethod;
        private static MethodInfo writeSettingsMethod;
        private static MethodInfo getFactionTerritoryColorMethod;
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

        private static bool PrepareFactionTerritoriesReflection()
        {
            if (factionTerritoriesSettingsType != null) return true;
            if (!ModsConfig.IsActive(FactionTerritoriesPackageId)) return false;

            var modType = AccessTools.TypeByName("FactionTerritories.FactionTerritoriesMod");
            if (modType == null)
            {
                Log.Error("Worldbuilder: Could not find type FactionTerritories.FactionTerritoriesMod");
                return false;
            }

            var instanceField = AccessTools.Field(modType, "Instance");
            if (instanceField == null)
            {
                Log.Error("Worldbuilder: Could not find Instance field on FactionTerritoriesMod");
                return false;
            }

            var modInstance = instanceField.GetValue(null);
            if (modInstance == null)
            {
                Log.Error("Worldbuilder: FactionTerritoriesMod.Instance is null");
                return false;
            }

            var settingsField = AccessTools.Field(modType, "Settings");
            if (settingsField == null)
            {
                Log.Error("Worldbuilder: Could not find Settings field on FactionTerritoriesMod");
                return false;
            }

            factionTerritoriesSettingsInstance = settingsField.GetValue(modInstance);
            if (factionTerritoriesSettingsInstance == null)
            {
                Log.Error("Worldbuilder: FactionTerritoriesMod.Settings is null");
                return false;
            }

            factionTerritoriesSettingsType = factionTerritoriesSettingsInstance.GetType();

            setOverrideColorMethod = AccessTools.Method(factionTerritoriesSettingsType, "SetOverrideColor", new Type[] { typeof(int), typeof(Color) });
            
            writeSettingsMethod = AccessTools.Method(typeof(ModSettings), "Write");

            var utilityType = AccessTools.TypeByName("FactionTerritories.FactionTerritoriesUtility");
            getFactionTerritoryColorMethod = AccessTools.Method(utilityType, "GetFactionTerritoryColor", new Type[] { typeof(Faction) });

            if (setOverrideColorMethod == null || getFactionTerritoryColorMethod == null)
            {
                Log.Error("Worldbuilder: Could not find required FactionTerritories methods");
                return false;
            }

            return true;
        }

        public static Color GetFactionTerritoryColor(Faction faction)
        {
            if (!PrepareFactionTerritoriesReflection()) return faction.Color;

            try
            {
                return (Color)getFactionTerritoryColorMethod.Invoke(null, new object[] { faction });
            }
            catch (Exception ex)
            {
                Log.Error("Worldbuilder: Failed to get faction territory color: " + ex);
                return faction.Color;
            }
        }

        public static bool TrySetFactionTerritoryColor(int factionLoadId, Color color)
        {
            if (!PrepareFactionTerritoriesReflection()) return false;

            try
            {
                setOverrideColorMethod.Invoke(factionTerritoriesSettingsInstance, new object[] { factionLoadId, color });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Worldbuilder: Failed to set faction territory color: " + ex);
                return false;
            }
        }

        public static void WriteFactionTerritorySettings()
        {
            if (!PrepareFactionTerritoriesReflection()) return;
            writeSettingsMethod?.Invoke(factionTerritoriesSettingsInstance, null);
        }

        public static void RequestFactionTerritoryRegenerate()
        {
            if (!ModsConfig.IsActive(FactionTerritoriesPackageId)) return;
            var utilityType = AccessTools.TypeByName("FactionTerritories.FactionTerritoriesUtility");
            var method = AccessTools.Method(utilityType, "RequestRegenerate", new Type[] { typeof(bool) });
            method?.Invoke(null, new object[] { true });
        }

        public static void TrySaveTerritoryColor(Faction faction, Color newColor)
        {
            if (faction == null || !ModsConfig.IsActive(FactionTerritoriesPackageId)) return;
            if (newColor == GetFactionTerritoryColor(faction)) return;
            TrySetFactionTerritoryColor(faction.loadID, newColor);
            WriteFactionTerritorySettings();
            RequestFactionTerritoryRegenerate();
        }
    }
}
