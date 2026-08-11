using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    public static partial class Page_CreateWorldParams_DoWindowContents_Patch
    {
        private static void DrawFactionSelection(
            Page_CreateWorldParams page,
            Rect rect)
        {
            var previousFloatMenu = Find.WindowStack.FloatMenu;
            WorldFactionsUIUtility.DoWindowContents(
                rect,
                page.factions,
                true);
            var openedFloatMenu = Find.WindowStack.FloatMenu;
            if (openedFloatMenu == null ||
                ReferenceEquals(openedFloatMenu, previousFloatMenu))
            {
                return;
            }

            Find.WindowStack.TryRemove(
                openedFloatMenu,
                doCloseSound: false);
            OpenFactionPicker(page.factions);
        }

        private static void OpenFactionPicker(List<FactionDef> factions)
        {
            var presentation = new DefPickerPresentation<FactionDef>(
                labelGetter: faction => faction.LabelCap.ToString(),
                tooltipGetter: faction =>
                    FactionPickerTooltip(faction, factions),
                iconGetter: faction => faction.FactionIcon,
                iconColorGetter: faction => faction.DefaultColor,
                acceptanceGetter: faction =>
                    CanAddFaction(faction, factions),
                iconAfterLabel: true);
            var picker = new Window_DefPicker<FactionDef>(
                "WB_SelectFaction".Translate(),
                FactionGenerator.ConfigurableFactions.Where(
                    faction => faction.displayInFactionSelection),
                faction =>
                {
                    if (CanAddFaction(faction, factions))
                    {
                        factions.Add(faction);
                    }
                },
                "WB_DefPickerNoEntries".Translate(),
                presentation)
            {
                soundAppear = null
            };
            Find.WindowStack.Add(picker);
        }

        private static string FactionPickerTooltip(
            FactionDef faction,
            List<FactionDef> factions)
        {
            var description = faction.Description;
            var acceptance = CanAddFaction(faction, factions);
            if (acceptance || acceptance.Reason.NullOrEmpty())
            {
                return description;
            }

            return description.NullOrEmpty()
                ? acceptance.Reason.CapitalizeFirst()
                : description + "\n\n" + acceptance.Reason.CapitalizeFirst();
        }

        private static AcceptanceReport CanAddFaction(
            FactionDef faction,
            List<FactionDef> factions)
        {
            if (factions.Count(
                    selectedFaction => selectedFaction == faction) >=
                faction.maxConfigurableAtWorldCreation)
            {
                return "MaxFactionsForType"
                    .Translate(faction.maxConfigurableAtWorldCreation)
                    .ToString()
                    .UncapitalizeFirst();
            }

            return true;
        }
    }
}
