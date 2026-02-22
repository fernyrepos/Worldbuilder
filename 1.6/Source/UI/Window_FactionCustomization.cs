using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_FactionCustomization : Window_WorldObjectCustomization
    {
        protected override bool ShowNarrativeTab => false;
        private Faction faction;
        private string currentFactionName = "";
        private string currentDescription = "";

        public Window_FactionCustomization(Faction faction) : base()
        {
            this.faction = faction;
            currentFactionName = faction.Name;
            currentDescription = World_ExposeData_Patch.individualFactionDescriptions.TryGetValue(faction.def, out var desc) ? desc : faction.def.GetPresetDescription();
            foreach (var kvp in World_ExposeData_Patch.individualFactionIcons)
            {
                if (kvp.Value == faction.def.factionIconPath)
                {
                    selectedFactionIconDef = kvp.Key;
                    break;
                }
            }
            selectedCulturalIconDef = World_ExposeData_Patch.individualFactionIdeoIcons.TryGetValue(faction.def, out var ideoIcon) ? ideoIcon : null;
            selectedColor = faction.color;
        }

        protected override void DrawDetailTab(Rect tabRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(tabRect);
            Text.Font = GameFont.Small;
            Rect factionLabelRect = listing.GetRect(24f);
            Widgets.Label(factionLabelRect, "WB_FactionNameLabel".Translate());

            Rect factionFieldRect = listing.GetRect(30f);
            currentFactionName = Widgets.TextField(factionFieldRect, currentFactionName);

            listing.Gap(12f);
            Rect descriptionLabelRect = listing.GetRect(24f);
            Widgets.Label(descriptionLabelRect, "WB_DescriptionLabel".Translate());

            Rect descriptionFieldRect = listing.GetRect(100f);
            currentDescription = Widgets.TextArea(descriptionFieldRect, currentDescription);

            listing.End();
        }

        protected override void SaveIndividualChanges()
        {
            if (string.IsNullOrWhiteSpace(currentFactionName))
            {
                Messages.Message("WB_FactionNameEmptyError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (faction.Name != currentFactionName)
            {
                faction.Name = currentFactionName;
                World_ExposeData_Patch.factionNamesById[faction.loadID] = currentFactionName;
            }

            if (!string.IsNullOrEmpty(currentDescription))
            {
                World_ExposeData_Patch.individualFactionDescriptions[faction.def] = currentDescription;
                World_ExposeData_Patch.factionDescriptionsById[faction.loadID] = currentDescription;
            }
            else
            {
                World_ExposeData_Patch.individualFactionDescriptions.Remove(faction.def);
                World_ExposeData_Patch.factionDescriptionsById.Remove(faction.loadID);
            }

            if (selectedFactionIconDef != null)
            {
                World_ExposeData_Patch.individualFactionIcons[faction.def] = selectedFactionIconDef.factionIconPath;
                World_ExposeData_Patch.individualFactionIdeoIcons.Remove(faction.def);
                Utils.ShowFactionIconSharedWarning(faction.def);
            }
            else if (selectedCulturalIconDef != null)
            {
                World_ExposeData_Patch.individualFactionIdeoIcons[faction.def] = selectedCulturalIconDef;
                World_ExposeData_Patch.individualFactionIcons.Remove(faction.def);
                Utils.ShowFactionIconSharedWarning(faction.def);
            }
            else
            {
                World_ExposeData_Patch.individualFactionIcons.Remove(faction.def);
                World_ExposeData_Patch.individualFactionIdeoIcons.Remove(faction.def);
            }

            if (selectedColor.HasValue)
            {
                faction.color = selectedColor.Value;
            }

            Messages.Message("WB_FactionCustomizationSaved".Translate(faction.Name), MessageTypeDefOf.PositiveEvent);
            Close();
        }
    }
}
