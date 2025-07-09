using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System;

namespace Worldbuilder
{
    [HotSwappable]
    public class WorldbuilderMod : Mod
    {
        public static WorldbuilderSettings settings;
        public WorldbuilderMod(ModContentPack pack) : base(pack)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                settings = GetSettings<WorldbuilderSettings>();
            });
            var harmony = new Harmony("WorldbuilderMod");
            harmony.PatchAll();
            ApplyGraphicPatches(harmony);
        }

        public static void ApplyGraphicPatches(Harmony harmony)
        {
            var postfix = new HarmonyMethod(typeof(WorldbuilderMod), nameof(GraphicGetterPostfix));
            var thingType = typeof(Thing);
            int patchedCount = 0;

            var relevantTypes = GenTypes.AllSubclasses(thingType);
            foreach (var type in relevantTypes)
            {
                try
                {
                    var graphicProperty = type.GetProperty("Graphic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (graphicProperty != null && graphicProperty.CanRead)
                    {
                        var getter = graphicProperty.GetGetMethod(true);
                        if (getter != null && getter.DeclaringType == type)
                        {
                            harmony.Patch(getter, postfix: postfix);
                            patchedCount++;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public static void GraphicGetterPostfix(Thing __instance, ref Graphic __result)
        {
            var data = __instance.GetCustomizationData();
            if (data != null)
            {
                var customGraphic = data.GetGraphic(__instance);
                __instance.LogMessage("Applying customization data, customGraphic:" + customGraphic);
                if (customGraphic != null && customGraphic != __result)
                {
                    __result = customGraphic;
                }
            }
            else
            {
                __instance.LogMessage("No customization data");
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            if (listingStandard.ButtonText("WB_SettingsResetPlayerDefaults".Translate()))
            {
                if (Current.Game == null)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox("WB_SettingsResetNeedsSave".Translate()));
                }
                else
                {
                    var confirmationDialog = Dialog_MessageBox.CreateConfirmation(
                        "WB_SettingsResetPlayerDefaultsConfirm".Translate(),
                        () =>
                        {
                            CustomizationDataCollections.playerDefaultCustomizationData.Clear();
                            Messages.Message("WB_SettingsResetPlayerDefaultsDone".Translate(), MessageTypeDefOf.PositiveEvent);
                        }
                    );
                    Find.WindowStack.Add(confirmationDialog);
                }
            }
            listingStandard.Gap(12f);

            if (listingStandard.ButtonText("WB_SettingsDeletePreset".Translate()))
            {
                List<WorldPreset> userPresets = WorldPresetManager.GetAllPresets().ToList();
                if (!userPresets.Any())
                {
                    Messages.Message("WB_SettingsNoPresetsToDelete".Translate(), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (WorldPreset preset in userPresets)
                    {
                        WorldPreset localPreset = preset;
                        options.Add(new FloatMenuOption(localPreset.name, () => ConfirmDeletePreset(localPreset.name)));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            listingStandard.Gap(12f);


            if (Current.Game != null)
            {
                if (listingStandard.ButtonText("WB_SettingsResaveWorld".Translate()))
                {
                    var presetToResave = WorldPresetManager.CurrentlyLoadedPreset;
                    if (presetToResave != null)
                    {
                        SaveWorldDataToPreset(presetToResave);
                        if (WorldPresetManager.SavePreset(presetToResave, null, null))
                        {
                            Messages.Message($"Worldbuilder: Successfully resaved world data to preset '{presetToResave.name}'.", MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            Messages.Message($"Worldbuilder: Failed to resave world data to preset '{presetToResave.name}'. Check logs.", MessageTypeDefOf.NegativeEvent);
                        }
                    }
                    else
                    {
                        Messages.Message("Worldbuilder: No world preset loaded to resave to.", MessageTypeDefOf.RejectInput);
                    }
                }
            }
            else
            {
                listingStandard.Label("WB_SettingsResaveWorldDesc".Translate());
            }


            listingStandard.Gap(24f);

            listingStandard.Label("WB_SettingsGizmoVisibilitySection".Translate());
            listingStandard.CheckboxLabeled("WB_SettingsShowGizmoThings".Translate(), ref settings.showCustomizeGizmoOnThings, "WB_SettingsShowGizmoThingsDesc".Translate());
            listingStandard.CheckboxLabeled("WB_SettingsShowGizmoPawns".Translate(), ref settings.showCustomizeGizmoOnPawns, "WB_SettingsShowGizmoPawnsDesc".Translate());
            listingStandard.CheckboxLabeled("WB_SettingsShowGizmoPlayerColony".Translate(), ref settings.showCustomizeGizmoOnPlayerColony, "WB_SettingsShowGizmoPlayerColonyDesc".Translate());
            listingStandard.CheckboxLabeled("WB_SettingsShowGizmoFactionBases".Translate(), ref settings.showCustomizeGizmoOnFactionBases, "WB_SettingsShowGizmoFactionBasesDesc".Translate());
            listingStandard.CheckboxLabeled("WB_SettingsShowGizmoMapMarkers".Translate(), ref settings.showCustomizeGizmoOnMapMarkers, "WB_SettingsShowGizmoMapMarkersDesc".Translate());

            listingStandard.Gap(24f);

            settings.pawnPortraitSize = listingStandard.SliderLabeled("WB_SettingsPawnPortraitSize".Translate() + ": " + settings.pawnPortraitSize.ToString("F0"), settings.pawnPortraitSize, 50f, 300f);


            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        private void ConfirmDeletePreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return;

            Dialog_MessageBox confirmationDialog = Dialog_MessageBox.CreateConfirmation(
                "WB_SettingsConfirmDeletePresetMessage".Translate(presetName),
                () =>
                {
                    if (WorldPresetManager.DeletePreset(presetName))
                    {
                        Messages.Message("WB_SettingsPresetDeletedSuccess".Translate(presetName), MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("WB_SettingsPresetDeleteFailed".Translate(presetName), MessageTypeDefOf.NegativeEvent);
                    }
                }
            );
            Find.WindowStack.Add(confirmationDialog);
        }

        public override string SettingsCategory()
        {
            return Content.Name;
        }
        public static void SaveWorldDataToPreset(WorldPreset presetToSaveTo)
        {
            if (presetToSaveTo == null)
            {
                Messages.Message("Worldbuilder: Cannot save world data, preset is null.", MessageTypeDefOf.RejectInput);
                return;
            }
            if (Current.Game == null || Find.World == null || Find.WorldGrid == null)
            {
                Messages.Message("Worldbuilder: Cannot save world data, no active game found.", MessageTypeDefOf.RejectInput);
                return;
            }


            if (presetToSaveTo.saveFactions)
            {
                presetToSaveTo.savedFactionDefs = Find.FactionManager.AllFactionsListForReading
                    .Where(f => !f.IsPlayer)
                    .Select(f => f.def)
                    .Distinct()
                    .ToList();
            }

            if (presetToSaveTo.saveIdeologies)
            {
                presetToSaveTo.savedIdeoFactionMapping = new Dictionary<string, List<string>>();
                try
                {
                    string presetDir = WorldPresetManager.GetPresetFolder(presetToSaveTo.name);
                    string ideosDir = Path.Combine(presetDir, "CustomIdeos");

                    if (Directory.Exists(ideosDir))
                    {
                        DirectoryInfo di = new DirectoryInfo(ideosDir);
                        foreach (FileInfo file in di.GetFiles("*.rid")) { file.Delete(); }
                    }
                    else { Directory.CreateDirectory(ideosDir); }

                    var allFactions = Find.FactionManager.AllFactionsListForReading;
                    foreach (var ideo in Find.IdeoManager.IdeosListForReading)
                    {
                        string safeName = GenText.SanitizeFilename(ideo.name + "_" + ideo.id);
                        string filePath = Path.Combine(ideosDir, safeName + GenFilePaths.IdeoExtension);
                        GameDataSaveLoader.SaveIdeo(ideo, filePath);
                        var associatedFactionDefNames = allFactions
                            .Where(f => f.ideos?.PrimaryIdeo == ideo)
                            .Select(f => f.def.defName)
                            .Distinct()
                            .ToList();

                        if (associatedFactionDefNames.Any())
                        {
                            presetToSaveTo.savedIdeoFactionMapping[safeName] = associatedFactionDefNames;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Worldbuilder: Error saving ideos/mapping for preset '{presetToSaveTo.name}': {ex}");
                }
            }

            if (presetToSaveTo.saveStorykeeperEntries)
            {
                presetToSaveTo.presetStories = World_ExposeData_Patch.worldStories.ListFullCopy();
            }
            else { presetToSaveTo.presetStories?.Clear(); }

            if (presetToSaveTo.saveTerrain)
            {
                presetToSaveTo.savedPlanetName = Find.World.info.name;
                presetToSaveTo.savedPlanetCoverage = Find.World.info.planetCoverage;
                presetToSaveTo.savedSeedString = Find.World.info.seedString;
                presetToSaveTo.savedPersistentRandomValue = Find.World.info.persistentRandomValue;
                presetToSaveTo.rainfall = Find.World.info.overallRainfall;
                presetToSaveTo.temperature = Find.World.info.overallTemperature;
                presetToSaveTo.population = Find.World.info.overallPopulation;
                presetToSaveTo.landmarkDensity = Find.World.info.landmarkDensity;
                presetToSaveTo.worldInfo = Find.World.info;
                var terrainData = new WorldPresetTerrainData
                {
                    savedWorldGrid = Find.WorldGrid
                };
                WorldPresetManager.SaveTerrainData(presetToSaveTo.name, terrainData);
            }
            else
            {
                WorldPresetManager.DeleteTerrainData(presetToSaveTo.name);
            }
            if (presetToSaveTo.saveBases)
            {
                presetToSaveTo.savedSettlementsData = Utils.GetSurfaceWorldObjects<Settlement>()
                    .Select(s =>
                    {
                        var saveData = new SettlementSaveData
                        {
                            tileID = s.Tile,
                            factionDefName = s.Faction?.def?.defName,
                            name = s.Name,
                            data = s.GetCustomizationData()?.Copy()
                        };
                        return saveData;
                    }).ToList();
            }
            else { presetToSaveTo.savedSettlementsData?.Clear(); }

            if (presetToSaveTo.saveMapMarkers)
            {
                presetToSaveTo.savedMapMarkersData = Utils.GetSurfaceWorldObjects<WorldObject_MapMarker>().Select(m =>
                {
                    var saveData = new MapMarkerSaveData
                    {
                        tileID = m.Tile,
                        markerData = MarkerDataManager.GetData(m)?.Copy()
                    };
                    return saveData;
                }).ToList();
            }
            else { presetToSaveTo.savedMapMarkersData?.Clear(); }

            if (presetToSaveTo.saveWorldFeatures)
            {
                presetToSaveTo.savedWorldFeaturesData = Find.World.features.features
                    .Where(f => f.def == DefsOf.WB_MapLabelFeature)
                    .Select(f => new MapTextSaveData { tileID = GetTileIdForFeature(f), labelText = f.name })
                    .ToList();
            }
            else { presetToSaveTo.savedWorldFeaturesData?.Clear(); }
            if (ModCompatibilityHelper.TryGetMLPSubcount(out int mlpSubcount))
            {
                presetToSaveTo.myLittlePlanetSubcount = mlpSubcount;
            }
            else
            {
                presetToSaveTo.myLittlePlanetSubcount = 10;
            }
            if (ModCompatibilityHelper.TryGetWTL(out TechLevel wtlValue))
            {
                presetToSaveTo.worldTechLevel = wtlValue;
            }
            else
            {
                presetToSaveTo.worldTechLevel = TechLevel.Undefined;
            }
            World_ExposeData_Patch.worldPresetName = presetToSaveTo.name;
            ApplyCustomizationsToExistingThings();
        }

        public static void ApplyCustomizationsToExistingThings()
        {
            var targetDef = Find.Selector.SingleSelectedThing?.def;
            if (targetDef is null)
            {
                return;
            }
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                LongEventHandler.toExecuteWhenFinished.Add(delegate
                {
                    foreach (Map map in Find.Maps)
                    {
                        if (targetDef != null)
                        {
                            var thingsToUpdate = map.listerThings.ThingsOfDef(targetDef);
                            foreach (Thing thing in thingsToUpdate)
                            {
                                CustomizationData customizationData = thing.GetCustomizationData();
                                if (customizationData != null)
                                {
                                    customizationData.SetGraphic(thing);
                                }
                            }
                        }
                    }
                });
            });
        }

        private static int GetTileIdForFeature(WorldFeature feature)
        {
            if (feature == null) return -1;
            for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
            {
                if (Find.WorldGrid[i].feature == feature) return i;
            }
            return -1;
        }
    }
}
