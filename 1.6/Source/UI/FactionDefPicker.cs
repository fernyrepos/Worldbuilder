using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal static class FactionDefPicker
    {
        internal static void Open(
            Action<FactionDef> onSelect,
            Func<FactionDef, AcceptanceReport> acceptanceGetter = null)
        {
            var presentation = new DefPickerPresentation<FactionDef>(
                labelGetter: faction => faction.LabelCap.ToString(),
                tooltipGetter: faction => GetTooltip(
                    faction,
                    acceptanceGetter),
                iconGetter: faction => faction.FactionIcon,
                iconColorGetter: faction => faction.DefaultColor,
                acceptanceGetter: acceptanceGetter,
                groupKeyGetter: faction => faction.techLevel.ToString(),
                groupLabelGetter: faction =>
                    faction.techLevel.ToStringHuman().CapitalizeFirst(),
                groupOrderGetter: faction =>
                    faction.techLevel == TechLevel.Undefined
                        ? int.MaxValue
                        : (int)faction.techLevel,
                groupIconGetter: faction =>
                    GetTechLevelIcon(faction.techLevel),
                allGroupsLabel: "WB_AllTechLevels".Translate(),
                useGroupFilter: true);
            var picker = new Window_DefPicker<FactionDef>(
                "WB_SelectFaction".Translate(),
                FactionGenerator.ConfigurableFactions.Where(
                    faction => faction.displayInFactionSelection),
                onSelect,
                "WB_DefPickerNoEntries".Translate(),
                presentation)
            {
                soundAppear = null
            };
            Find.WindowStack.Add(picker);
        }

        private static string GetTooltip(
            FactionDef faction,
            Func<FactionDef, AcceptanceReport> acceptanceGetter)
        {
            var description = faction.Description;
            if (acceptanceGetter == null)
            {
                return description;
            }

            var acceptance = acceptanceGetter(faction);
            if (acceptance || acceptance.Reason.NullOrEmpty())
            {
                return description;
            }

            return description.NullOrEmpty()
                ? acceptance.Reason.CapitalizeFirst()
                : description + "\n\n" +
                  acceptance.Reason.CapitalizeFirst();
        }

        private static Texture2D GetTechLevelIcon(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.Animal:
                    return Resources.TechLevel_Animal;
                case TechLevel.Neolithic:
                    return Resources.TechLevel_Neolithic;
                case TechLevel.Medieval:
                    return Resources.TechLevel_Medieval;
                case TechLevel.Industrial:
                    return Resources.TechLevel_Industrial;
                case TechLevel.Spacer:
                    return Resources.TechLevel_Spacer;
                case TechLevel.Ultra:
                    return Resources.TechLevel_Ultra;
                case TechLevel.Archotech:
                    return Resources.TechLevel_Archotech;
                default:
                    return null;
            }
        }
    }
}
