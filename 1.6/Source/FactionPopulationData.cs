using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Worldbuilder
{
    public class FactionPopulationData : IExposable
    {
        public string pawnSingular;
        public string pawnsPlural;
        public string leaderTitle;
        public TechLevel? techLevel;
        public bool? permanentEnemy;
        public bool disableMemeRequirements;
        public bool forceXenotypeOverride;
        public List<XenotypeChance> xenotypeChances;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnSingular, "pawnSingular");
            Scribe_Values.Look(ref pawnsPlural, "pawnsPlural");
            Scribe_Values.Look(ref leaderTitle, "leaderTitle");
            Scribe_Values.Look(ref techLevel, "techLevel");
            Scribe_Values.Look(ref permanentEnemy, "permanentEnemy");
            Scribe_Values.Look(ref disableMemeRequirements, "disableMemeRequirements", defaultValue: false);
            Scribe_Values.Look(ref forceXenotypeOverride, "forceXenotypeOverride", defaultValue: false);
            Scribe_Collections.Look(ref xenotypeChances, "xenotypeChances", LookMode.Deep);
        }
    }
}
