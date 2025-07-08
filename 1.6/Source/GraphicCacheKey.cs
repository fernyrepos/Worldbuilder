using Verse;
using UnityEngine;
using System;

namespace Worldbuilder
{
    public struct GraphicCacheKey : IEquatable<GraphicCacheKey>
    {
        public Color? color;

        public Color colorTwo;
        public ThingStyleDef styleDef;
        public int? variationIndex;
        public string selectedImagePath;
        public ThingDef def;

        public ThingDef stuff;
        public GraphicCacheKey(Color? color, Color colorTwo, ThingStyleDef styleDef, int? variationIndex, string selectedImagePath, ThingDef def, ThingDef stuff)
        {
            this.color = color;
            this.colorTwo = colorTwo;
            this.styleDef = styleDef;
            this.variationIndex = variationIndex;
            this.selectedImagePath = selectedImagePath;
            this.def = def;
            this.stuff = stuff;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + color.GetHashCode();
                hash = hash * 23 + colorTwo.GetHashCode();
                hash = hash * 23 + (styleDef?.GetHashCode() ?? 0);
                hash = hash * 23 + variationIndex.GetHashCode();
                hash = hash * 23 + (selectedImagePath?.GetHashCode() ?? 0);
                hash = hash * 23 + (def?.GetHashCode() ?? 0);
                hash = hash * 23 + (stuff?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public bool Equals(GraphicCacheKey other)
        {
            return color == other.color &&
                   colorTwo == other.colorTwo &&
                   styleDef == other.styleDef &&
                   variationIndex == other.variationIndex &&
                   selectedImagePath == other.selectedImagePath &&
                   def == other.def &&
                   stuff == other.stuff;
        }

        public override bool Equals(object obj)
        {
            return obj is GraphicCacheKey other && Equals(other);
        }
        public static bool operator ==(GraphicCacheKey a, GraphicCacheKey b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GraphicCacheKey a, GraphicCacheKey b)
        {
            return !(a == b);
        }
    }
}
