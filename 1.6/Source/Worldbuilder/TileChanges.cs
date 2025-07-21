using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;

namespace Worldbuilder
{
    public class TileChanges : IExposable
    {
        public BiomeDef biome;
        public Hilliness hilliness;
        public List<LandmarkDef> landmarks = new List<LandmarkDef>();
        public List<TileMutatorDef> features = new List<TileMutatorDef>();

        public void ExposeData()
        {
            Scribe_Defs.Look(ref biome, "biome");
            Scribe_Values.Look(ref hilliness, "hilliness", Hilliness.Undefined);
            Scribe_Collections.Look(ref landmarks, "landmarks", LookMode.Def);
            Scribe_Collections.Look(ref features, "features", LookMode.Def);
        }
    }
}
