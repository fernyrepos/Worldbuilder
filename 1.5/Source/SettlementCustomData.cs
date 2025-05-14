using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    public class SettlementCustomData : IExposable
    {
        public string narrativeText = "";
        public string selectedFactionIconDefName;
        public string selectedCulturalIconDefName;
        public string description;
        public Color? color;
        private Material cachedMaterial;

        public FactionDef SelectedFactionIconDef => string.IsNullOrEmpty(selectedFactionIconDefName) ? null : DefDatabase<FactionDef>.GetNamed(selectedFactionIconDefName, false);
        public IdeoIconDef SelectedCulturalIconDef => string.IsNullOrEmpty(selectedCulturalIconDefName) ? null : DefDatabase<IdeoIconDef>.GetNamed(selectedCulturalIconDefName, false);

        public void ExposeData()
        {
            Scribe_Values.Look(ref narrativeText, "narrativeText", "");
            Scribe_Values.Look(ref selectedFactionIconDefName, "selectedFactionIconDefName");
            Scribe_Values.Look(ref selectedCulturalIconDefName, "selectedCulturalIconDefName");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref color, "color");
        }

        public SettlementCustomData Copy()
        {
            return new SettlementCustomData
            {
                narrativeText = this.narrativeText,
                selectedFactionIconDefName = this.selectedFactionIconDefName,
                selectedCulturalIconDefName = this.selectedCulturalIconDefName,
                description = this.description,
                color = this.color
            };
        }

        public void ClearMaterialCache()
        {
            cachedMaterial = null;
        }

        public Material GetMaterial(Settlement settlement)
        {
            if (settlement == null) return null;
            if (cachedMaterial != null)
            {
                return cachedMaterial;
            }
            Texture2D customIconTex = null;
            if (!string.IsNullOrEmpty(selectedCulturalIconDefName))
            {
                IdeoIconDef culturalIconDef = SelectedCulturalIconDef;
                if (culturalIconDef != null)
                {
                    customIconTex = culturalIconDef.Icon;
                }
            }
            if (customIconTex == null && !string.IsNullOrEmpty(selectedFactionIconDefName))
            {
                FactionDef factionIconDef = SelectedFactionIconDef;
                if (factionIconDef != null && !string.IsNullOrEmpty(factionIconDef.factionIconPath))
                {
                    customIconTex = ContentFinder<Texture2D>.Get(factionIconDef.factionIconPath, false);
                }
            }
            if (customIconTex != null)
            {
                if (color.HasValue)
                {
                    cachedMaterial = MaterialPool.MatFrom(customIconTex, ShaderDatabase.WorldOverlayTransparentLit,
                        color.Value, WorldMaterials.WorldObjectRenderQueue);
                }
                else
                {
                    cachedMaterial = MaterialPool.MatFrom(customIconTex, ShaderDatabase.WorldOverlayTransparentLit,
                        settlement.Faction.Color, WorldMaterials.WorldObjectRenderQueue);
                }

                return cachedMaterial;
            }

            return null;
        }
    }
}
