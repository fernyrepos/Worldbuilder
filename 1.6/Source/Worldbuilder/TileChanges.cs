using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;

namespace Worldbuilder
{
    public class TileChanges : IExposable
    {
        public string biome;
        public Hilliness hilliness;
        public List<string> landmarks = new List<string>();
        public List<string> features = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref biome, "biome");
            Scribe_Values.Look(ref hilliness, "hilliness", Hilliness.Undefined);
            Scribe_Collections.Look(ref landmarks, "landmarks", LookMode.Value);
            Scribe_Collections.Look(ref features, "features", LookMode.Value);
        }
    }
}
