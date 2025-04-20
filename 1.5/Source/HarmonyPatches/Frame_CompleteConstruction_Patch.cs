using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Frame), "CompleteConstruction")]
    public static class Frame_CompleteConstruction_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo spawnMethod = AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) });
            MethodInfo helperMethod = AccessTools.Method(typeof(CustomizationDataCollections), nameof(CustomizationDataCollections.TryAssignPlayerDefault));


            bool foundSpawn = false;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(spawnMethod))
                {
                    foundSpawn = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5);
                    yield return new CodeInstruction(OpCodes.Call, helperMethod);
                }

                yield return instruction;
            }

            if (!foundSpawn)
            {
                Log.Error("Worldbuilder: Frame.CompleteConstruction transpiler did not find GenSpawn.Spawn call.");
            }
        }
    }
}
