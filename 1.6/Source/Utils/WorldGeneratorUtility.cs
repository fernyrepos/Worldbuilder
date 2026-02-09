using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Profile;

namespace Worldbuilder
{
    public static class WorldGeneratorUtility
    {
        public static void GenerateWorldFromPreset(WorldPreset preset, Action onFinished)
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                Find.GameInitData.ResetWorldRelatedMapInitData();
                string seed = preset?.saveTerrain == true
                                ? preset.worldInfo.seedString
                                : GenText.RandomSeedString();

                float coverage = preset?.saveTerrain == true
                                ? preset.worldInfo.planetCoverage
                                : ((!Prefs.DevMode || !UnityData.isEditor) ? 0.3f : 0.05f);
                OverallRainfall rain = preset?.saveTerrain == true
                                ? preset.worldInfo.overallRainfall
                                : OverallRainfall.Normal;

                OverallTemperature temp = preset?.saveTerrain == true
                                ? preset.worldInfo.overallTemperature
                                : OverallTemperature.Normal;

                OverallPopulation pop = preset?.saveTerrain == true
                                ? preset.worldInfo.overallPopulation
                                : OverallPopulation.Normal;

                float pollutionParam = preset?.saveTerrain == true
                                ? preset.worldInfo.pollution
                                : (ModsConfig.BiotechActive ? 0.05f : 0f);

                var landmarkDensity = preset?.saveTerrain == true
                    ? preset.worldInfo.landmarkDensity
                    : LandmarkDensity.Normal;

                List<FactionDef> factionsToGenerate;
                if (preset?.saveFactions == true && preset.savedFactionDefs != null)
                {
                    factionsToGenerate = preset.savedFactionDefs.ToDefs<FactionDef>();
                }
                else
                {
                    factionsToGenerate = new List<FactionDef>();
                    foreach (var configurableFaction in FactionGenerator.ConfigurableFactions)
                    {
                        if (configurableFaction.startingCountAtWorldCreation > 0)
                        {
                            for (int i = 0; i < configurableFaction.startingCountAtWorldCreation; i++)
                            {
                                factionsToGenerate.Add(configurableFaction);
                            }
                        }
                    }
                    foreach (var faction in FactionGenerator.ConfigurableFactions)
                    {
                        if (faction.replacesFaction != null)
                        {
                            factionsToGenerate.RemoveAll((FactionDef x) => x == faction.replacesFaction);
                        }
                    }
                }
                Current.Game.World = WorldGenerator.GenerateWorld(coverage, seed, rain, temp, pop, landmarkDensity, factionsToGenerate, pollutionParam);

                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    onFinished?.Invoke();
                    MemoryUtility.UnloadUnusedUnityAssets();
                    Find.World.renderer.RegenerateAllLayersNow();
                });
            }, "GeneratingWorld", doAsynchronously: true, null);
        }
    }
}
