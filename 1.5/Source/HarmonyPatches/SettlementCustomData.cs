using RimWorld;
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
    }
}
