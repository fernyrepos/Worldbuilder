using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(WorldGenStep_Roads), nameof(WorldGenStep_Roads.GenerateRoadEndpoints))]
public static class WorldGenStep_Roads_GenerateRoadEndpoints_Void_Patch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var methodToHook = AccessTools.Method(typeof(FloatRange), $"get_{nameof(FloatRange.RandomInRange)}");
        var codes = instructions.ToList();
        var found = false;
        foreach (var code in codes)
        {
            yield return code;
            if (found || !code.Calls(methodToHook))
            {
                continue;
            }

            yield return new CodeInstruction(OpCodes.Ldsfld,
                AccessTools.Field(typeof(World_ExposeData_Patch),
                    nameof(World_ExposeData_Patch.worldGenerationData)));
            yield return new CodeInstruction(OpCodes.Ldfld,
                AccessTools.Field(typeof(WorldGenerationData), nameof(WorldGenerationData.factionRoadDensity)));
            yield return new CodeInstruction(OpCodes.Mul);
            found = true;
        }
    }
}
