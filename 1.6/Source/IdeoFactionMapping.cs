using Verse;
using System.Collections.Generic;

namespace Worldbuilder
{
    public class IdeoFactionMapping : IExposable
    {
        public List<string> factionDefNames = new List<string>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref factionDefNames, "factionDefNames", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                factionDefNames ??= new List<string>();
            }
        }
    }
}
