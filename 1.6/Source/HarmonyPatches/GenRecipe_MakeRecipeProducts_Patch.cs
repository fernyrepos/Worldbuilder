using HarmonyLib;
using Verse;
using System.Collections.Generic;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class GenRecipe_MakeRecipeProducts_Patch
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker)
        {
            foreach (var t in __result)
            {
                CustomizationDataCollections.TryAssignPlayerDefault(worker, t);
                yield return t;
            }
        }
    }
}
