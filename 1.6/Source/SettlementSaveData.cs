using Verse;
using RimWorld;
using Worldbuilder;

public class SettlementSaveData : IExposable
{
    public int tileID = -1;
    public FactionDef faction;
    public string name;
    public SettlementCustomData data;

    public void ExposeData()
    {
        Scribe_Values.Look(ref tileID, "tileID", -1);
        Scribe_Defs.Look(ref faction, "faction");
        Scribe_Values.Look(ref name, "name");
        Scribe_Deep.Look(ref data, "data");
    }
}
