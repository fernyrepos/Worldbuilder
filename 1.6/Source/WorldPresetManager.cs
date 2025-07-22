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
        private static readonly string PresetFileName = "Preset.xml";
        static WorldPresetManager()
        {
            Directory.CreateDirectory(BasePresetFolderPath);
        }

        public static string GetPresetFolder(string presetName)
        {
            return Path.Combine(BasePresetFolderPath, presetName);
        }

        private static string GetPresetFilePath(string presetName)
        {
            return Path.Combine(GetPresetFolder(presetName), PresetFileName);
        }

        public static string GetThumbnailPath(string presetName)
        {
            return Path.Combine(GetPresetFolder(presetName), "Thumbnail.png");
        }

        public static string GetFlavorImagePath(string presetName)
        {
            return Path.Combine(GetPresetFolder(presetName), "Flavor.png");
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

                string presetFilePath = Path.Combine(dirPath, PresetFileName);
                if (File.Exists(presetFilePath))
                {
                    try
                    {
                        WorldPreset loadedPreset = LoadPresetFromFile(presetFilePath);
                        if (loadedPreset != null)
                        {
                            loadedPreset.presetFolder = dirPath;
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

                    string presetFilePath = Path.Combine(dirPath, PresetFileName);
                    if (File.Exists(presetFilePath))
                    {
                        try
                        {
                            WorldPreset loadedPreset = LoadPresetFromFile(presetFilePath);
                            if (loadedPreset != null)
                            {
                                loadedPreset.presetFolder = dirPath;
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

            string presetFolderPath = GetPresetFolder(preset.name);
            string filePath = GetPresetFilePath(preset.name);

            try
            {
                Directory.CreateDirectory(presetFolderPath);
                try
                {
                    if (thumbnailBytes != null && thumbnailBytes.Length > 0)
                    {
                        string destThumbnailPath = Path.Combine(presetFolderPath, "Thumbnail.png");
                        File.WriteAllBytes(destThumbnailPath, thumbnailBytes);
                    }
                    if (flavorImageBytes != null && flavorImageBytes.Length > 0)
                    {
                        string destFlavorPath = Path.Combine(presetFolderPath, "Flavor.png");
                        File.WriteAllBytes(destFlavorPath, flavorImageBytes);
                    }
                }
                catch (IOException ioEx)
                {
                    Log.Error($"Worldbuilder: Error writing preset images for '{preset.name}': {ioEx.Message}");
                }
                if (preset.customizationDefaults != null)
                {
                    string customImagesPath = Path.Combine(presetFolderPath, "CustomImages");
                    List<ThingDef> keys = preset.customizationDefaults.Keys.ToList().ToDefs<ThingDef>();

                    if (preset.presetFolder.NullOrEmpty() is false)
                    {
                        var presetCustomImageFolder = Path.Combine(preset.presetFolder, "CustomImages");
                        if (presetCustomImageFolder != customImagesPath && Directory.Exists(presetCustomImageFolder))
                        {
                            if (Directory.Exists(customImagesPath) is false)
                            {
                                Directory.CreateDirectory(customImagesPath);
                            }
                            foreach (var file in Directory.GetFiles(presetCustomImageFolder))
                            {
                                var fileName = Path.GetFileName(file);
                                var destPath = Path.Combine(customImagesPath, fileName);
                                File.Copy(file, destPath, true);
                            }
                        }
                    }

                    foreach (ThingDef def in keys)
                    {
                        CustomizationData data = preset.customizationDefaults[def.defName];
                        if (!string.IsNullOrEmpty(data.selectedImagePath) && !data.selectedImagePath.StartsWith("CustomImages/") && File.Exists(data.selectedImagePath))
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
                                data.selectedImagePath = "CustomImages/" + newFilename;
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

        public static bool DeletePreset(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Log.Error($"Worldbuilder: Cannot delete preset with null or empty name.");
                return false;
            }

            string presetFolderPath = GetPresetFolder(name);
            string presetFilePath = GetPresetFilePath(name);
            if (!Directory.Exists(presetFolderPath))
            {
                presetsCache?.RemoveAll(p => p.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
                return false;
            }

            try
            {
                TryDeleteFile(Path.Combine(presetFolderPath, "Thumbnail.png"), name);
                TryDeleteFile(Path.Combine(presetFolderPath, "Flavor.png"), name);
                TryDeleteDirectory(Path.Combine(presetFolderPath, "CustomImages"), name);

                if (Directory.Exists(presetFolderPath))
                {
                    Directory.Delete(presetFolderPath, recursive: true);
                }

                presetsCache?.RemoveAll(p => p.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
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
