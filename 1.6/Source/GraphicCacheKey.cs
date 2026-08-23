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
        public float rotation;
        public Vector2 offset;
        public AltitudeLayer? layer;
        public float drawScale;
        public float alpha;
        public GraphicCacheKey(Color? color, Color colorTwo, ThingStyleDef styleDef, int? variationIndex, string selectedImagePath, ThingDef def, ThingDef stuff, float rotation, Vector2 offset, AltitudeLayer? layer, float drawScale, float alpha)
        {
            this.color = color;
            this.colorTwo = colorTwo;
            this.styleDef = styleDef;
            this.variationIndex = variationIndex;
            this.selectedImagePath = selectedImagePath;
            this.def = def;
            this.stuff = stuff;
            this.rotation = rotation;
            this.offset = offset;
            this.layer = layer;
            this.drawScale = drawScale;
            this.alpha = alpha;
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
                hash = hash * 23 + rotation.GetHashCode();
                hash = hash * 23 + offset.GetHashCode();
                hash = hash * 23 + layer.GetHashCode();
                hash = hash * 23 + drawScale.GetHashCode();
                hash = hash * 23 + alpha.GetHashCode();
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
                   stuff == other.stuff &&
                   rotation == other.rotation &&
                   offset == other.offset &&
                   layer == other.layer &&
                   drawScale == other.drawScale &&
                   alpha == other.alpha;
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
