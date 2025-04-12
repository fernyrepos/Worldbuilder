using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Worldbuilder
{
    public class IdeoWithFactions : IExposable
    {
        public string ideoName;
        public string cultureDefName;
        public List<string> memeDefNames;

        public List<FactionDef> factionDefs;

        [Unsaved]
        public Ideo ideo;

        public void ExposeData()
        {
            Scribe_Values.Look(ref ideoName, "ideoName");
            Scribe_Values.Look(ref cultureDefName, "cultureDefName");
            Scribe_Collections.Look(ref memeDefNames, "memeDefNames", LookMode.Value);

            Scribe_Collections.Look(ref factionDefs, "factionDefs", LookMode.Def);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                factionDefs ??= new List<FactionDef>();
                memeDefNames ??= new List<string>();
            }
        }
    }
}