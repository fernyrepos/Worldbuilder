using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_PopulationEditor : Window
    {
        private readonly Faction faction;
        private Vector2 scrollPosition;

        private readonly List<XenotypeChance> tempXenotypes = new List<XenotypeChance>();
        private string tempSingular;
        private string tempPlural;
        private string tempLeaderTitle;
        private TechLevel tempTechLevel;
        private Ideo tempIdeo;
        private int tempGoodwill;
        private bool tempPermanentEnemy;
        private bool tempDisableMemeRequirements;
        private bool tempForceXenotypeOverride;
        public override Vector2 InitialSize => new Vector2(750f, 650f);

        public Window_PopulationEditor(Faction faction)
        {
            this.faction = faction;
            InitializeTempValues();

            doCloseX = true;
            forcePause = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        private void InitializeTempValues()
        {
            var data = faction.GetPopulationData();

            var sourceXenotypes = data?.xenotypeChances;
            if (sourceXenotypes != null)
            {
                tempXenotypes.AddRange(sourceXenotypes.Select(x => new XenotypeChance(x.xenotype, x.chance)));
            }
            else if (faction.def.xenotypeSet != null)
            {
                float totalWeight = faction.def.BaselinerChance;
                for (int i = 0; i < faction.def.xenotypeSet.Count; i++)
                {
                    if (faction.def.xenotypeSet[i].xenotype != XenotypeDefOf.Baseliner)
                    {
                        totalWeight += faction.def.xenotypeSet[i].chance;
                    }
                }

                tempXenotypes.Add(new XenotypeChance(XenotypeDefOf.Baseliner, faction.def.BaselinerChance / totalWeight));
                for (int i = 0; i < faction.def.xenotypeSet.Count; i++)
                {
                    var xenoChance = faction.def.xenotypeSet[i];
                    if (xenoChance.xenotype != XenotypeDefOf.Baseliner)
                    {
                        tempXenotypes.Add(new XenotypeChance(xenoChance.xenotype, xenoChance.chance / totalWeight));
                    }
                }
            }
            else
            {
                tempXenotypes.Add(new XenotypeChance(XenotypeDefOf.Baseliner, 1.0f));
            }

            tempSingular = data?.pawnSingular ?? faction.def.pawnSingular;
            tempPlural = data?.pawnsPlural ?? faction.def.pawnsPlural;
            tempLeaderTitle = data?.leaderTitle ?? faction.LeaderTitle;
            tempTechLevel = data?.techLevel ?? faction.def.techLevel;
            tempIdeo = faction.ideos?.PrimaryIdeo;
            tempGoodwill = faction.IsPlayer ? 0 : faction.PlayerGoodwill;
            tempPermanentEnemy = data?.permanentEnemy ?? faction.def.permanentEnemy;
            tempDisableMemeRequirements = data?.disableMemeRequirements ?? false;
            tempForceXenotypeOverride = data?.forceXenotypeOverride ?? false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "WB_EditPopulation".Translate() + ": " + faction.Name);
            Text.Font = GameFont.Small;

            Rect mainRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 95f);
            Widgets.DrawMenuSection(mainRect);

            Rect leftRect = new Rect(mainRect.x + 15, mainRect.y + 15, 300, mainRect.height - 30);
            Rect rightRect = new Rect(leftRect.xMax + 15, mainRect.y + 15, mainRect.width - leftRect.width - 45, mainRect.height - 30);

            DrawXenotypeEditor(leftRect);
            DrawDetailsEditor(rightRect);

            Rect bottomRect = new Rect(inRect.x, inRect.yMax - 35f, inRect.width, 35f);

            if (Widgets.ButtonText(new Rect(bottomRect.x, bottomRect.y, 150f, bottomRect.height), "Cancel".Translate()))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(bottomRect.xMax - 150f, bottomRect.y, 150f, bottomRect.height), "Save".Translate()))
            {
                Save();
            }
        }

        private void DrawXenotypeEditor(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            listing.Label("WB_PopEditor_Xenotypes".Translate());
            listing.Gap(4);

            Rect scrollViewRect = listing.GetRect(rect.height - 100f - 32f);
            float viewHeight = tempXenotypes.Count * 32f;
            Rect viewRect = new Rect(0, 0, scrollViewRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(scrollViewRect, ref scrollPosition, viewRect);
            for (int i = tempXenotypes.Count - 1; i >= 0; i--)
            {
                var xeno = tempXenotypes[i];
                Rect rowRect = new Rect(0, i * 32f, viewRect.width, 30f);

                Rect iconRect = new Rect(rowRect.x, rowRect.y, 30f, 30f);
                Widgets.DrawTextureFitted(iconRect, xeno.xenotype.Icon, 1f);

                Rect labelRect = new Rect(iconRect.xMax + 5f, rowRect.y, 80f, 30f);
                Widgets.Label(labelRect, xeno.xenotype.LabelCap);
                TooltipHandler.TipRegion(labelRect, xeno.xenotype.description);

                Rect deleteRect = new Rect(rowRect.xMax - 24f, rowRect.y + 3, 24f, 24f);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    tempXenotypes.RemoveAt(i);
                    break;
                }

                Rect sliderRect = new Rect(labelRect.xMax + 5f, rowRect.y, deleteRect.x - labelRect.xMax - 10f, 30f);
                float percentage = xeno.chance * 100f;
                percentage = Widgets.HorizontalSlider(sliderRect, percentage, 0f, 100f, true, Mathf.RoundToInt(percentage).ToString() + "%");
                xeno.chance = Mathf.RoundToInt(percentage) / 100f;
            }
            Widgets.EndScrollView();

            Rect bottomControlsRect = new Rect(rect.x - 15, scrollViewRect.yMax + 70, rect.width, 30f);

            Rect warningLabelRect = new Rect(bottomControlsRect.x, bottomControlsRect.y, bottomControlsRect.width - 50f, 40f);
            Widgets.Label(warningLabelRect, "WB_PopEditor_XenotypeSaveWarning".Translate());

            Rect addButtonRect = new Rect(warningLabelRect.xMax, bottomControlsRect.y, 30f, 30f);
            if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
            {
                var options = new List<FloatMenuOption>();
                foreach (var xenoDef in DefDatabase<XenotypeDef>.AllDefs.OrderBy(x => x.label))
                {
                    if (tempXenotypes.All(x => x.xenotype != xenoDef))
                    {
                        options.Add(new FloatMenuOption(xenoDef.LabelCap, () => {
                            tempXenotypes.Add(new XenotypeChance(xenoDef, 0));
                        }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.End();
        }

        private void DrawDetailsEditor(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            var singularLabelRect = listing.GetRect(24f);
            Widgets.Label(singularLabelRect, "WB_PopEditor_SingularMember".Translate());
            var singularFieldRect = listing.GetRect(30f);
            tempSingular = Widgets.TextField(singularFieldRect, tempSingular);

            var pluralLabelRect = listing.GetRect(24f);
            Widgets.Label(pluralLabelRect, "WB_PopEditor_PluralMember".Translate());
            var pluralFieldRect = listing.GetRect(30f);
            tempPlural = Widgets.TextField(pluralFieldRect, tempPlural);

            var leaderTitleLabelRect = listing.GetRect(24f);
            Widgets.Label(leaderTitleLabelRect, "WB_PopEditor_LeaderTitle".Translate());
            var leaderTitleFieldRect = listing.GetRect(30f);
            tempLeaderTitle = Widgets.TextField(leaderTitleFieldRect, tempLeaderTitle);
            listing.Gap();
            if (listing.ButtonTextLabeled("WB_PopEditor_TechLevel".Translate(), tempTechLevel.ToStringHuman().CapitalizeFirst()))
            {
                Find.WindowStack.Add(new FloatMenu(System.Enum.GetValues(typeof(TechLevel))
                    .Cast<TechLevel>()
                    .Where(t => t != TechLevel.Undefined)
                    .Select(t => new FloatMenuOption(t.ToStringHuman().CapitalizeFirst(), () => tempTechLevel = t))
                    .ToList()));
            }

            if (listing.ButtonTextLabeled("WB_PopEditor_Ideoligion".Translate(), tempIdeo?.name ?? "None".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(Find.IdeoManager.IdeosListForReading
                    .OrderBy(i => i.name)
                    .Select(i => new FloatMenuOption(i.name, () => tempIdeo = i))
                    .ToList()));
            }

            listing.Label("WB_PopEditor_Relations".Translate() + ": " + tempGoodwill);
            tempGoodwill = (int)listing.Slider(tempGoodwill, -100, 100);

            listing.CheckboxLabeled("WB_PopEditor_PermanentEnemy".Translate(), ref tempPermanentEnemy);
            listing.Gap(6f);

            listing.CheckboxLabeled("WB_PopEditor_DisablePreferences".Translate(), ref tempDisableMemeRequirements);
            var originalColor = GUI.color;
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            listing.Label("WB_PopEditor_DisablePreferencesDesc".Translate());
            Text.Font = GameFont.Small;
            GUI.color = originalColor;
            listing.Gap(6f);

            listing.CheckboxLabeled("WB_PopEditor_ForceXenotype".Translate(), ref tempForceXenotypeOverride);
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            listing.Label("WB_PopEditor_ForceXenotypeDesc".Translate());
            Text.Font = GameFont.Small;
            GUI.color = originalColor;

            listing.End();
        }

        private void Save()
        {
            float sum = tempXenotypes.Sum(x => x.chance);
            if (!Mathf.Approximately(sum, 1f))
            {
                Messages.Message("WB_XenotypeTotalError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            var popData = CustomizationDataCollections.factionPopulationData.TryGetValue(faction, out var d) ? d : new FactionPopulationData();

            popData.pawnSingular = tempSingular;
            popData.pawnsPlural = tempPlural;
            popData.leaderTitle = tempLeaderTitle;
            popData.techLevel = tempTechLevel;
            popData.permanentEnemy = tempPermanentEnemy;
            popData.disableMemeRequirements = tempDisableMemeRequirements;
            popData.forceXenotypeOverride = tempForceXenotypeOverride;
            popData.xenotypeChances = tempXenotypes.Where(x => x.chance > 0.0001f).ToList();

            CustomizationDataCollections.factionPopulationData[faction] = popData;

            World_ExposeData_Patch.ApplyPopulationCustomization(faction.def, popData);

            if (faction.ideos != null && tempIdeo != null)
                faction.ideos.SetPrimary(tempIdeo);

            if (!faction.IsPlayer)
            {
                int goodwillDiff = tempGoodwill - faction.PlayerGoodwill;
                if (goodwillDiff != 0)
                    faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwillDiff, canSendMessage: false);
            }

            Messages.Message("WB_PopulationSaveSuccess".Translate(faction.Name), MessageTypeDefOf.PositiveEvent);
            Close();
        }
    }
}
