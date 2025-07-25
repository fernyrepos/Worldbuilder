using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(IdeoManager), "SortIdeos")]
    public static class IdeoManager_SortIdeos_Patch
    {
        private static bool isLoadingPresetIdeos = false;
        public static void Postfix(IdeoManager __instance)
        {
            if (isLoadingPresetIdeos) return;

            var loadingPreset = WorldPresetManager.CurrentlyLoadedPreset;
            if (loadingPreset == null || !loadingPreset.saveIdeologies)
            {
                return;
            }

            isLoadingPresetIdeos = true;

            try
            {
                string presetDir = loadingPreset.PresetFolder;
                string ideosDir = Path.Combine(presetDir, "CustomIdeos");

                if (Directory.Exists(ideosDir))
                {
                    DirectoryInfo di = new DirectoryInfo(ideosDir);

                    foreach (FileInfo file in di.GetFiles("*.rid"))
                    {
                        if (GameDataSaveLoader.TryLoadIdeo(file.FullName, out Ideo loadedIdeo))
                        {
                            IdeoGenerator.InitLoadedIdeo(loadedIdeo);
                            {
                                var existingIdeo = __instance.IdeosListForReading.FirstOrDefault(existingIdeo =>
                                    existingIdeo.name == loadedIdeo.name &&
                                    existingIdeo.culture == loadedIdeo.culture &&
                                    existingIdeo.memes.Count == loadedIdeo.memes.Count &&
                                    !existingIdeo.memes.Except(loadedIdeo.memes).Any()
                                );

                                if (existingIdeo is null)
                                {
                                    loadedIdeo.id = Find.UniqueIDsManager.GetNextIdeoID();
                                    __instance.Add(loadedIdeo);
                                }
                                else
                                {
                                    LongEventHandler.ExecuteWhenFinished(delegate
                                    {
                                        existingIdeo.SetIcon(loadedIdeo.iconDef, loadedIdeo.colorDef);
                                        existingIdeo.primaryFactionColor = loadedIdeo.primaryFactionColor;
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Error loading custom ideos from preset '{loadingPreset.name}': {ex}");
            }
            finally
            {
                isLoadingPresetIdeos = false;
            }
        }
    }
}
