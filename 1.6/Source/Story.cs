using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    public class Story : IExposable
    {
        public string title;
        public string text;
        public IdeoIconDef iconDef;
        public Color iconColor = Color.white;

        private Texture2D cachedIcon;

        public void ExposeData()
        {
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref text, "text");
            Scribe_Defs.Look(ref iconDef, "iconDef");
            Scribe_Values.Look(ref iconColor, "iconColor", Color.white);
        }

        public void ClearIconCache()
        {
            cachedIcon = null;
        }

        public Texture2D CoverIcon
        {
            get
            {
                if (cachedIcon == null && iconDef != null)
                {
                    cachedIcon = iconDef.Icon;
                }
                return cachedIcon ?? GizmoUtility.NarrativeGizmoIcon;
            }
        }

        public override string ToString()
        {
            return title + " - " + text;
        }
    }
}
