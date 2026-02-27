using RimWorld.Planet;
using Verse;
namespace Worldbuilder
{
    public class MapTextSaveData : IExposable
    {
        public PlanetTile tileID = PlanetTile.Invalid;
        public string labelText;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tileID, "tileID", PlanetTile.Invalid);
            Scribe_Values.Look(ref labelText, "labelText");
        }
    }
}
