using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    public class SettlementCustomData : WorldObjectData
    {
        public string factionDescription;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref factionDescription, "factionDescription");
        }

        public SettlementCustomData Copy()
        {
            return new SettlementCustomData
            {
                narrativeText = this.narrativeText,
                factionIconDef = this.factionIconDef,
                iconDef = this.iconDef,
                description = this.description,
                factionDescription = this.factionDescription,
                color = this.color
            };
        }
    }
}
