using Verse;
using RimWorld;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld.Planet;
using System.Diagnostics;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public static class WorldPresetManager
    {
        private static List<WorldPreset> presetsCache;
        private static WorldPreset _currentlyLoadedPreset;
        public static WorldPreset CurrentlyLoadedPreset
        {
            get
            {
                var targetPresetName = World_ExposeData_Patch.WorldPresetName;
                if (targetPresetName.NullOrEmpty())
                {
                    return null;
                }
                if (targetPresetName != _currentlyLoadedPreset?.name)
                {
                    if (Scribe.mode != LoadSaveMode.Inactive)
                    {
                        //Log.Error("Worldbuilder: CurrentlyLoadedPreset accessed during load. This is not supported.");
                        return null;
                    }
                    _currentlyLoadedPreset = GetAllPresets().FirstOrDefault(p => p.name == targetPresetName);
                }
                return _currentlyLoadedPreset;
            }
            set
            {
                _currentlyLoadedPreset = value;
            }
        }
        private static readonly string BasePresetFolderPath = GenFilePaths.FolderUnderSaveData("Worldbuilder");
        static WorldPresetManager()
        {
            Directory.CreateDirectory(BasePresetFolderPath);
        }


        public static bool SaveTerrainData(WorldPreset preset, WorldPresetTerrainData terrainData)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.name))
            {
                Log.Error("Worldbuilder: Cannot save terrain data with null or empty preset name.");
                return false;
            }
            if (terrainData == null)
            {
                Log.Error($"Worldbuilder: Cannot save terrain data for preset '{preset.name}', terrainData is null.");
                return false;
            }

            string filePath = preset.TerrainDataFilePath;
            try
            {
                Directory.CreateDirectory(preset.PresetFolder);
                SafeSaver.Save(filePath, "terrainData", () =>
                {
                    terrainData.ExposeData();
                });
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Failed to save terrain data to {filePath}: {ex.Message}");
                return false;
            }
        }

        public static WorldPresetTerrainData LoadTerrainData(WorldPreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.name))
            {
                Log.Error("Worldbuilder: Cannot load terrain data with null or empty preset name.");
                return null;
            }

            string filePath = preset.TerrainDataFilePath;
            if (!File.Exists(filePath))
            {
                return null;
            }

            WorldPresetTerrainData loadedData = new WorldPresetTerrainData();
            Scribe.loader.InitLoading(filePath);
            try
            {
                loadedData.ExposeData();
            }
            catch (System.Exception ex)
            {
                Scribe.loader.ForceStop();
                Log.Error($"Worldbuilder: Failed to load terrain data from {filePath}: {ex.Message}");
                return null;
            }
            finally
            {
                Scribe.loader.FinalizeLoading();
            }
            return loadedData;
        }

        public static bool DeleteTerrainData(WorldPreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.name))
            {
                Log.Error($"Worldbuilder: Cannot delete terrain data with null or empty preset name.");
                return false;
            }

            string filePath = preset.TerrainDataFilePath;
            if (!File.Exists(filePath))
            {
                return true;
            }

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Failed to delete terrain data file '{filePath}' for preset '{preset.name}': {ex.Message}");
                return false;
            }
        }
        public static List<WorldPreset> GetAllPresets(bool forceReload = false)
        {
            if (presetsCache != null && !forceReload)
            {
                return presetsCache;
            }

            presetsCache = new List<WorldPreset>();
            var loadedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            Directory.CreateDirectory(BasePresetFolderPath);
            foreach (string dirPath in Directory.GetDirectories(BasePresetFolderPath))
            {
                string dirName = Path.GetFileName(dirPath);
                if (string.IsNullOrEmpty(dirName)) continue;

                string presetFilePath = Path.Combine(dirPath, WorldPreset.PresetFileName);
                if (File.Exists(presetFilePath))
                {
                    try
                    {
                        WorldPreset loadedPreset = LoadPresetFromFile(presetFilePath);
                        if (loadedPreset != null)
                        {
                            loadedPreset.PresetFolder = dirPath;
                            if (loadedPreset.name == null || !loadedPreset.name.Equals(dirName, System.StringComparison.OrdinalIgnoreCase))
                            {
                                loadedPreset.name = dirName;
                            }
                            if (loadedNames.Add(loadedPreset.name))
                            {
                                presetsCache.Add(loadedPreset);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"Worldbuilder: Failed to load user world preset from {presetFilePath}: {ex.Message}");
                    }
                }
            }

            foreach (ModContentPack mod in LoadedModManager.RunningMods)
            {
                string modPresetBaseDir = Path.Combine(mod.RootDir, "Worldbuilder");
                if (!Directory.Exists(modPresetBaseDir)) continue;

                foreach (string dirPath in Directory.GetDirectories(modPresetBaseDir))
                {
                    string dirName = Path.GetFileName(dirPath);
                    if (string.IsNullOrEmpty(dirName)) continue;

                    string presetFilePath = Path.Combine(dirPath, WorldPreset.PresetFileName);
                    if (File.Exists(presetFilePath))
                    {
                        try
                        {
                            WorldPreset loadedPreset = LoadPresetFromFile(presetFilePath);
                            if (loadedPreset != null)
                            {
                                loadedPreset.PresetFolder = dirPath;
                                if (loadedPreset.name == null || !loadedPreset.name.Equals(dirName, System.StringComparison.OrdinalIgnoreCase))
                                {
                                    loadedPreset.name = dirName;
                                }
                                if (loadedNames.Add(loadedPreset.name))
                                {
                                    presetsCache.Add(loadedPreset);
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"Worldbuilder: Failed to load mod world preset from {presetFilePath} (Mod: {mod.Name}): {ex.Message}");
                        }
                    }
                }
            }

            return presetsCache;
        }

        public static WorldPreset GetPreset(string name)
        {
            GetAllPresets();
            return presetsCache.FirstOrDefault(p => p.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        public static bool SavePreset(WorldPreset preset, byte[] thumbnailBytes, byte[] flavorImageBytes)
        {
            if (string.IsNullOrWhiteSpace(preset?.name))
            {
                Log.Error("Worldbuilder: Cannot save preset with null or empty name.");
                return false;
            }

            string presetFolderPath = preset.PresetFolder;
            string filePath = preset.PresetFilePath;

            try
            {
                Directory.CreateDirectory(presetFolderPath);
                try
                {
                    if (thumbnailBytes != null && thumbnailBytes.Length > 0)
                    {
                        File.WriteAllBytes(preset.ThumbnailPath, thumbnailBytes);
                    }
                    if (flavorImageBytes != null && flavorImageBytes.Length > 0)
                    {
                        File.WriteAllBytes(preset.FlavorImagePath, flavorImageBytes);
                    }
                }
                catch (IOException ioEx)
                {
                    Log.Error($"Worldbuilder: Error writing preset images for '{preset.name}': {ioEx.Message}");
                }
                if (preset.customizationDefaults != null)
                {
                    string customImagesPath = preset.CustomImagesPath;
                    List<ThingDef> keys = preset.customizationDefaults.Keys.ToList().ToDefs<ThingDef>();

                    if (Directory.Exists(customImagesPath) is false)
                    {
                        Directory.CreateDirectory(customImagesPath);
                    }

                    foreach (ThingDef def in keys)
                    {
                        CustomizationData data = preset.customizationDefaults[def.defName];
                        if (data.IsExternalImage)
                        {
                            try
                            {
                                if (Directory.Exists(customImagesPath) is false)
                                {
                                    Directory.CreateDirectory(customImagesPath);
                                }
                                string newFilename = def.defName + ".png";
                                string destPath = Path.Combine(customImagesPath, newFilename);
                                File.Copy(data.selectedImagePath, destPath, true);
                                data.selectedImagePath = WorldPreset.CustomImagesFolderName + "/" + newFilename;
                            }
                            catch (IOException ioEx)
                            {
                                Log.Error($"Worldbuilder: Error copying custom image '{data.selectedImagePath}' for def '{def.defName}' in preset '{preset.name}': {ioEx.Message}");
                            }
                            catch (System.Exception ex)
                            {
                                Log.Error($"Worldbuilder: Unexpected error processing custom image for def '{def.defName}' in preset '{preset.name}': {ex.Message}");
                            }
                        }
                    }
                }

                SafeSaver.Save(filePath, "worldPreset", () =>
                {
                    preset.ExposeData();
                });

                presetsCache?.RemoveAll(p => p.name.Equals(preset.name, System.StringComparison.OrdinalIgnoreCase));
                presetsCache?.Add(preset);
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Failed to save world preset to {filePath}: {ex.Message}");
                return false;
            }
        }

        public static bool DeletePreset(WorldPreset preset)
        {
            string presetFolderPath = preset.PresetFolder;
            if (!Directory.Exists(presetFolderPath))
            {
                presetsCache?.RemoveAll(p => p.name.Equals(preset.name, System.StringComparison.OrdinalIgnoreCase));
                return false;
            }

            try
            {
                TryDeleteFile(preset.ThumbnailPath, preset.name);
                TryDeleteFile(preset.FlavorImagePath, preset.name);
                TryDeleteDirectory(preset.CustomImagesPath, preset.name);

                if (Directory.Exists(presetFolderPath))
                {
                    Directory.Delete(presetFolderPath, recursive: true);
                }

                presetsCache?.RemoveAll(p => p.name.Equals(preset.name, System.StringComparison.OrdinalIgnoreCase));
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Failed during deletion process for preset folder '{presetFolderPath}': {ex.Message}");
                return false;
            }
        }

        public static bool shouldPrevent;
        private static WorldPreset LoadPresetFromFile(string fullFilePath)
        {
            WorldPreset loadedPreset = new WorldPreset();
            Scribe.loader.InitLoading(fullFilePath);
            shouldPrevent = true;
            try
            {
                loadedPreset.ExposeData();
            }
            catch (System.Exception ex)
            {
                Scribe.loader.ForceStop();
                throw ex;
            }
            finally
            {
                Scribe.loader.FinalizeLoading();
                shouldPrevent = false;
            }
            return loadedPreset;
        }

        private static void TryDeleteFile(string filePath, string presetNameForLog)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (System.Exception)
                {
                    Log.Error($"Worldbuilder: Failed to delete file '{filePath}' for preset '{presetNameForLog}'.");
                }
            }
        }
        private static void TryDeleteDirectory(string dirPath, string presetNameForLog)
        {
            if (!string.IsNullOrEmpty(dirPath) && Directory.Exists(dirPath))
            {
                try
                {
                    Directory.Delete(dirPath, recursive: true);
                }
                catch (System.Exception)
                {
                    Log.Error($"Worldbuilder: Failed to delete directory '{dirPath}' for preset '{presetNameForLog}'.");
                }
            }
        }
    }
}
