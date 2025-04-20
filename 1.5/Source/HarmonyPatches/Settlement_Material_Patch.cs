using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.Material), MethodType.Getter)]
    public static class Settlement_Material_Patch
    {
        public static void Postfix(WorldObject __instance, ref Material __result)
        {
            Settlement settlement = (Settlement)__instance;
            var customData = SettlementCustomDataManager.GetData(settlement) ?? World_ExposeData_Patch.GetPresetSettlementCustomizationData(settlement);

            if (customData != null)
            {
                Texture2D customIconTex = null;
                if (!string.IsNullOrEmpty(customData.selectedCulturalIconDefName))
                {
                    IdeoIconDef culturalIconDef = customData.SelectedCulturalIconDef;
                    if (culturalIconDef != null)
                    {
                        customIconTex = culturalIconDef.Icon;
                    }
                }
                if (customIconTex == null && !string.IsNullOrEmpty(customData.selectedFactionIconDefName))
                {
                    FactionDef factionIconDef = customData.SelectedFactionIconDef;
                    if (factionIconDef != null && !string.IsNullOrEmpty(factionIconDef.factionIconPath))
                    {
                        customIconTex = ContentFinder<Texture2D>.Get(factionIconDef.factionIconPath, false);
                    }
                }
                if (customIconTex != null)
                {
                    __result = MaterialPool.MatFrom(customIconTex);
                }
            }
        }
    }
}
