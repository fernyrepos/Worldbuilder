using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_FactionCustomization : Window
    {
        private Faction faction;
        private string currentFactionName = "";
        private string currentDescription = "";
        private string currentIconPath = "";
        private Color? selectedColor;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, 450f);
            }
        }

        public Window_FactionCustomization(Faction faction)
        {
            this.faction = faction;
            forcePause = true;
            doCloseX = true;
            currentFactionName = faction.Name;
            currentDescription = World_ExposeData_Patch.individualFactionDescriptions.TryGetValue(faction.def, out var desc) ? desc : faction.def.GetPresetDescription();
            currentIconPath = World_ExposeData_Patch.individualFactionIcons.TryGetValue(faction.def, out var icon) ? icon : "";
            selectedColor = faction.color;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            Text.Font = GameFont.Small;

            var nameLabelRect = listing.GetRect(24f);
            Widgets.Label(nameLabelRect, "WB_FactionNameLabel".Translate());

            var nameFieldRect = listing.GetRect(30f);
            currentFactionName = Widgets.TextField(nameFieldRect, currentFactionName);

            listing.Gap(12f);
            var descriptionLabelRect = listing.GetRect(24f);
            Widgets.Label(descriptionLabelRect, "WB_DescriptionLabel".Translate());

            var descriptionFieldRect = listing.GetRect(100f);
            currentDescription = Widgets.TextArea(descriptionFieldRect, currentDescription);

            listing.Gap(12f);
            var iconLabelRect = listing.GetRect(24f);
            Widgets.Label(iconLabelRect, "WB_FactionIconLabel".Translate());

            var iconFieldRect = listing.GetRect(30f);
            currentIconPath = Widgets.TextField(iconFieldRect, currentIconPath);

            listing.Gap(12f);
            var colorLabelRect = listing.GetRect(24f);
            Widgets.Label(colorLabelRect, "WB_FactionColorLabel".Translate());

            var colorRect = listing.GetRect(30f);
            if (Widgets.ButtonText(colorRect, "WB_FactionColorPicker".Translate()))
            {
                Find.WindowStack.Add(new Window_ColorPicker(selectedColor ?? Color.white, (Color color) => selectedColor = color));
            }

            if (selectedColor.HasValue)
            {
                var colorPreviewRect = new Rect(colorRect.xMax + 10f, colorRect.y, 30f, 30f);
                Widgets.DrawBoxSolid(colorPreviewRect, selectedColor.Value);
            }

            listing.Gap(30f);
            var saveButtonRect = listing.GetRect(30f);
            if (Widgets.ButtonText(saveButtonRect, "WB_CustomizeSave".Translate()))
            {
                SaveChanges();
            }

            listing.End();
        }

        private void SaveChanges()
        {
            if (string.IsNullOrWhiteSpace(currentFactionName))
            {
                Messages.Message("WB_FactionNameEmptyError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (faction.Name != currentFactionName)
            {
                faction.Name = currentFactionName;
            }

            if (!string.IsNullOrEmpty(currentDescription))
            {
                World_ExposeData_Patch.individualFactionDescriptions[faction.def] = currentDescription;
            }
            else
            {
                World_ExposeData_Patch.individualFactionDescriptions.Remove(faction.def);
            }

            if (!string.IsNullOrEmpty(currentIconPath))
            {
                World_ExposeData_Patch.individualFactionIcons[faction.def] = currentIconPath;
                World_ExposeData_Patch.individualFactionIdeoIcons.Remove(faction.def);
                Utils.ShowFactionIconSharedWarning(faction.def);
            }
            else
            {
                World_ExposeData_Patch.individualFactionIcons.Remove(faction.def);
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
