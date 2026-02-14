using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class WorldObjectData : IExposable
    {
        public string narrativeText = "";
        public string description;
        public Color? color;
        public string name;
        public IdeoIconDef iconDef;
        public FactionDef factionIconDef;
        private Texture2D cachedIconTexture;
        public string syncedFilePath;
        public bool syncToExternalFile;
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref narrativeText, "narrativeText", "");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref color, "color");
            Scribe_Values.Look(ref name, "name");
            Scribe_Defs.Look(ref iconDef, "iconDef");
            Scribe_Defs.Look(ref factionIconDef, "factionIconDef");
            Scribe_Values.Look(ref syncedFilePath, "syncedFilePath");
            Scribe_Values.Look(ref syncToExternalFile, "syncToExternalFile", defaultValue: false);
        }

        public void ClearIconCache()
        {
            cachedIconTexture = null;
        }

        public Texture2D GetIcon()
        {
            if (cachedIconTexture != null)
            {
                return cachedIconTexture;
            }
            if (iconDef != null)
            {
                cachedIconTexture = iconDef.Icon;
                if (cachedIconTexture != null)
                {
                    return cachedIconTexture;
                }
            }
            if (factionIconDef != null && !string.IsNullOrEmpty(factionIconDef.factionIconPath))
            {
                cachedIconTexture = ContentFinder<Texture2D>.Get(factionIconDef.factionIconPath, false);
                if (cachedIconTexture != null)
                {
                    return cachedIconTexture;
                }
            }

            return null;
        }
    }
}
