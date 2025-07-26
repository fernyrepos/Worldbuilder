using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(FactionGenerator), nameof(FactionGenerator.GenerateFactionsIntoWorldLayer))]
    public static class FactionGenerator_GenerateFactionsIntoWorld_Patch
    {
        public static void Postfix()
        {
            var preset = WorldPresetManager.CurrentlyLoadedPreset;
            if (preset == null || !preset.saveFactions || preset.savedFactionDefs == null || preset.savedFactionDefs.Count == 0)
            {
                return;
            }

            foreach (var faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (faction.def == null || !preset.savedFactionDefs.Contains(faction.def.defName))
                {
                    continue;
                }

                string ideoFilename = null;
                if (preset.savedIdeoFactionMapping != null)
                {
                    foreach (var kvp in preset.savedIdeoFactionMapping)
                    {
                        if (kvp.Value != null && kvp.Value.Contains(faction.def.defName))
                        {
                            ideoFilename = kvp.Key;
                            break;
                        }
                    }
                }

                if (ideoFilename != null)
                {
                    string presetDir = preset.PresetFolder;
                    string ideosDir = Path.Combine(presetDir, "CustomIdeos");
                    string ideoFilePath = Path.Combine(ideosDir, ideoFilename + GenFilePaths.IdeoExtension);

                    if (File.Exists(ideoFilePath))
                    {
                        if (GameDataSaveLoader.TryLoadIdeo(ideoFilePath, out Ideo loadedIdeo))
                        {
                            IdeoGenerator.InitLoadedIdeo(loadedIdeo);
                            var loadedMemes = loadedIdeo.memes?.OrderBy(m => m.defName).ToList();
                            var existingIdeo = Find.IdeoManager.IdeosListForReading.FirstOrDefault(i =>
                                i.name == loadedIdeo.name &&
                                i.culture == loadedIdeo.culture &&
                                (i.memes?.OrderBy(m => m.defName).SequenceEqual(loadedMemes ?? Enumerable.Empty<MemeDef>()) ?? (loadedMemes == null || !loadedMemes.Any()))
                            );

                            if (existingIdeo != null)
                            {
                                var presetIdeo = loadedIdeo;
                                loadedIdeo = existingIdeo;
                                LongEventHandler.ExecuteWhenFinished(delegate
                                {
                                    existingIdeo.SetIcon(presetIdeo.iconDef, presetIdeo.colorDef);
                                    existingIdeo.primaryFactionColor = presetIdeo.primaryFactionColor;
                                });
                            }
                            else
                            {
                                loadedIdeo.id = Find.UniqueIDsManager.GetNextIdeoID();
                                Find.IdeoManager.Add(loadedIdeo);
                            }

                            if (faction.ideos == null)
                            {
                                faction.ideos = new FactionIdeosTracker(faction);
                            }
                            faction.ideos.SetPrimary(loadedIdeo);
                        }
                    }
                }
            }
        }
    }
}
