using Verse;
using UnityEngine;
using System;

namespace Worldbuilder
{
    public struct GraphicCacheKey : IEquatable<GraphicCacheKey>
    {
        public Color? color;
        public ThingStyleDef styleDef;
        public int? variationIndex;
        public string selectedImagePath;
        public ThingDef def;
        public GraphicCacheKey(Color? color, ThingStyleDef styleDef, int? variationIndex, string selectedImagePath, ThingDef def)
        {
            this.color = color;
            this.styleDef = styleDef;
            this.variationIndex = variationIndex;
            this.selectedImagePath = selectedImagePath;
            this.def = def;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + color.GetHashCode();
                hash = hash * 23 + (styleDef?.GetHashCode() ?? 0);
                hash = hash * 23 + variationIndex.GetHashCode();
                hash = hash * 23 + (selectedImagePath?.GetHashCode() ?? 0);
                hash = hash * 23 + (def?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public bool Equals(GraphicCacheKey other)
        {
            return color == other.color &&
                   styleDef == other.styleDef &&
                   variationIndex == other.variationIndex &&
                   selectedImagePath == other.selectedImagePath &&
                   def == other.def;
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