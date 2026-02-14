using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse.Noise;

namespace Worldbuilder;

[HarmonyPatch(typeof(WorldGenStep_Terrain), nameof(WorldGenStep_Terrain.GenerateTileFor))]
public static class WorldGenStep_Terrain_GenerateTileFor_Patch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var methodToHook = AccessTools.Method(typeof(ModuleBase), nameof(ModuleBase.GetValue), [typeof(Vector3)]);
        var noiseMountainLinesField =
            AccessTools.Field(typeof(WorldGenStep_Terrain), nameof(WorldGenStep_Terrain.noiseMountainLines));
        var codes = instructions.ToList();
        for (var i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            yield return code;
            if (i <= 2 || !code.Calls(methodToHook) || !codes[i - 2].LoadsField(noiseMountainLinesField))
            {
                continue;
            }

            yield return new CodeInstruction(OpCodes.Ldsfld,
                AccessTools.Field(typeof(Page_CreateWorldParams_DoWindowContents_Patch),
                    nameof(Page_CreateWorldParams_DoWindowContents_Patch.tmpGenerationData)));
            yield return new CodeInstruction(OpCodes.Ldfld,
                AccessTools.Field(typeof(WorldGenerationData), nameof(WorldGenerationData.mountainDensity)));
            yield return new CodeInstruction(OpCodes.Div);
        }
    }
}
