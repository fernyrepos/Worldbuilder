using Verse;
using Worldbuilder;

public class MapMarkerSaveData : IExposable
{
    public int tileID = -1;
    public MarkerData markerData;

    public void ExposeData()
    {
        Scribe_Values.Look(ref tileID, "tileID", -1);
        Scribe_Deep.Look(ref markerData, "markerData");
    }
}
