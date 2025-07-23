using Verse;

public class MapTextSaveData : IExposable
{
    public int tileID = -1;
    public string labelText;

    public void ExposeData()
    {
        Scribe_Values.Look(ref tileID, "tileID", -1);
        Scribe_Values.Look(ref labelText, "labelText");
    }
}
