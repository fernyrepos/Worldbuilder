using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(PawnGenerator), "GetXenotypeForGeneratedPawn")]
    [HotSwappable]
    public static class PawnGenerator_GetXenotypeForGeneratedPawn_Patch
    {
        public static bool Prefix(PawnGenerationRequest request, ref XenotypeDef __result)
        {
            if (request.Faction == null) return true;
            
            var data = request.Faction.GetPopulationData();
            if (data == null || data.xenotypeChances.NullOrEmpty()) return true;

            if (data.forceXenotypeOverride || request.KindDef.xenotypeSet == null)
            {
                 if (data.xenotypeChances.TryRandomElementByWeight(x => x.chance, out var chosen))
                {
                    __result = chosen.xenotype;
                    return false;
                }
            }
            return true;
        }
    }
}
