using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    public class MapMarkerSaveData : IExposable
    {
        public PlanetTile tileID = PlanetTile.Invalid;
        public MarkerData markerData;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tileID, "tileID", PlanetTile.Invalid);
            Scribe_Deep.Look(ref markerData, "markerData");
        }
    }
}
