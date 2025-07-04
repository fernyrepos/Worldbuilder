using RimWorld;
using RimWorld.Planet;
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
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref narrativeText, "narrativeText", "");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref color, "color");
            Scribe_Values.Look(ref name, "name");
            Scribe_Defs.Look(ref iconDef, "iconDef");
            Scribe_Defs.Look(ref factionIconDef, "factionIconDef");
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

    public class MarkerData : WorldObjectData
    {
        public MarkerData Copy()
        {
            return new MarkerData
            {
                name = this.name,
                description = this.description,
                narrativeText = this.narrativeText,
                iconDef = this.iconDef,
                factionIconDef = this.factionIconDef,
                color = this.color
            };
        }
    }

    public class SettlementCustomData : WorldObjectData
    {
        public string factionDescription;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref factionDescription, "factionDescription");
        }

        public SettlementCustomData Copy()
        {
            return new SettlementCustomData
            {
                narrativeText = this.narrativeText,
                factionIconDef = this.factionIconDef,
                iconDef = this.iconDef,
                description = this.description,
                factionDescription = this.factionDescription,
                color = this.color
            };
        }
    }
}
