using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [DefOf]
    public static class WorldbuilderDefOf
    {
        public static WorldObjectDef Worldbuilder_MapMarker;
        public static FeatureDef Worldbuilder_MapLabelFeature;

        static WorldbuilderDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WorldbuilderDefOf));
        }
    }
}