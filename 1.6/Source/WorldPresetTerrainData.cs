using Verse;
using RimWorld.Planet;

namespace Worldbuilder
{
    public class WorldPresetTerrainData : IExposable
    {
        public WorldGrid savedWorldGrid;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref savedWorldGrid, "savedWorldGrid");
        }
    }
}
