using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [DefOf]
    public static class DefsOf
    {
        public static WorldObjectDef WB_MapMarker;
        public static FeatureDef WB_MapLabelFeature;

        static DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefsOf));
        }
    }
}
