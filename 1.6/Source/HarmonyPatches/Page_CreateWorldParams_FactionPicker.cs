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
            FactionDefPicker.Open(
                faction =>
                {
                    if (CanAddFaction(faction, factions))
                    {
                        factions.Add(faction);
                    }
                },
                faction => CanAddFaction(faction, factions),
                faction => factions.Count(
                    selectedFaction => selectedFaction == faction));
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
