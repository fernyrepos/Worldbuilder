using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Color BackgroundColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 15);
        public static readonly Texture2D GeneratePreview = ContentFinder<Texture2D>.Get("UI/GeneratePreview");
        public static readonly Texture2D Visible = ContentFinder<Texture2D>.Get("UI/Visible");
        public static readonly Texture2D InVisible = ContentFinder<Texture2D>.Get("UI/InVisible");
        public static readonly Texture2D saveTexture2D = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");
        public static readonly Texture2D loadTexture2D = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarker");
        public static readonly Texture2D TechLevel_Unrestricted = ContentFinder<Texture2D>.Get("UI/Cancel");
        public static readonly Texture2D TechLevel_Animal = ContentFinder<Texture2D>.Get("UI/TechLevel_1");
        public static readonly Texture2D TechLevel_Neolithic = ContentFinder<Texture2D>.Get("UI/TechLevel_2");
        public static readonly Texture2D TechLevel_Medieval = ContentFinder<Texture2D>.Get("UI/TechLevel_3");
        public static readonly Texture2D TechLevel_Industrial = ContentFinder<Texture2D>.Get("UI/TechLevel_4");
        public static readonly Texture2D TechLevel_Spacer = ContentFinder<Texture2D>.Get("UI/TechLevel_5");
        public static readonly Texture2D TechLevel_Ultra = ContentFinder<Texture2D>.Get("UI/TechLevel_6");
        public static readonly Texture2D TechLevel_Archotech = ContentFinder<Texture2D>.Get("UI/TechLevel_7");
    }
}
