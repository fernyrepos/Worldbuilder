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
        private static Dictionary<string, WorldPreset> presetsCache;
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
                return presetsCache.Values.ToList();
            }

            presetsCache = new Dictionary<string, WorldPreset>(System.StringComparer.OrdinalIgnoreCase);
            Directory.CreateDirectory(BasePresetFolderPath);

            var allPresetDirs = new List<string>();
            allPresetDirs.AddRange(Directory.GetDirectories(BasePresetFolderPath));

            foreach (ModContentPack mod in LoadedModManager.RunningMods)
            {
                foreach (var folder in mod.foldersToLoadDescendingOrder)
                {
                    string modPresetBaseDir = Path.Combine(folder, "Worldbuilder");
                    if (Directory.Exists(modPresetBaseDir))
                    {
                        allPresetDirs.AddRange(Directory.GetDirectories(modPresetBaseDir));
                    }
                }
            }

            foreach (string dirPath in allPresetDirs)
            {
                try
                {
                    TryLoadPreset(dirPath);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Worldbuilder: Failed to process preset directory {dirPath}: {ex.Message}");
                }
            }
            return presetsCache.Values.ToList();
        }

        private static void TryLoadPreset(string dirPath)
        {
            string presetFilePath = Path.Combine(dirPath, WorldPreset.PresetFileName);
            string dirName = Path.GetFileName(dirPath);

            if (File.Exists(presetFilePath))
            {
                WorldPreset loadedPreset = LoadPresetFromFile(presetFilePath);
                if (loadedPreset != null)
                {
                    if (string.IsNullOrEmpty(loadedPreset.name))
                    {
                        loadedPreset.name = dirName;
                    }

                    if (presetsCache.TryGetValue(loadedPreset.name, out var existingPreset))
                    {
                        existingPreset.AddPathOverridesFrom(dirPath);
                    }
                    else
                    {
                        presetsCache[loadedPreset.name] = loadedPreset;
                        loadedPreset.PresetFolder = dirPath;
                    }
                }
            }
            else if (presetsCache.TryGetValue(dirName, out var existingPreset))
            {
                existingPreset.AddPathOverridesFrom(dirPath);
            }
        }

        public static WorldPreset GetPreset(string name)
        {
            GetAllPresets();
            presetsCache.TryGetValue(name, out var preset);
            return preset;
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

                if (presetsCache != null)
                {
                    presetsCache[preset.name] = preset;
                }
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
                presetsCache?.Remove(preset.name);
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

                presetsCache?.Remove(preset.name);
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
