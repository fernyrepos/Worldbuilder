using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    public class SettlementSaveData : IExposable
    {
        public PlanetTile tileID = PlanetTile.Invalid;
        public FactionDef faction;
        public string name;
        public SettlementCustomData data;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tileID, "tileID", PlanetTile.Invalid);
            Scribe_Defs.Look(ref faction, "faction");
            Scribe_Values.Look(ref name, "name");
            Scribe_Deep.Look(ref data, "data");
        }
    }
}
