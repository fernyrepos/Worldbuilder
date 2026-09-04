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

        private readonly List<ExposableXenotypeChance> tempXenotypes = new List<ExposableXenotypeChance>();
        private readonly Dictionary<ExposableXenotypeChance, string> xenotypeChanceBuffers = new Dictionary<ExposableXenotypeChance, string>();
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
                tempXenotypes.AddRange(sourceXenotypes.Select(x => new ExposableXenotypeChance(x.xenotype, x.chance)));
            }
            else if (faction.def.xenotypeSet != null)
            {

                float explicitSum = 0f;
                for (int i = 0; i < faction.def.xenotypeSet.Count; i++)
                {
                    var x = faction.def.xenotypeSet[i];
                    if (x.xenotype != XenotypeDefOf.Baseliner)
                    {
                        explicitSum += x.chance;
                    }
                }

                float baselinerRaw = 1f - explicitSum;
                var baselinerChance = Mathf.Max(0f, baselinerRaw);

                float totalWeight = explicitSum + baselinerChance;
                if (totalWeight <= 0f) totalWeight = 1f;

                tempXenotypes.Add(new ExposableXenotypeChance(XenotypeDefOf.Baseliner, baselinerChance / totalWeight));

                for (int i = 0; i < faction.def.xenotypeSet.Count; i++)
                {
                    var xenoChance = faction.def.xenotypeSet[i];
                    if (xenoChance.xenotype != XenotypeDefOf.Baseliner)
                    {
                        tempXenotypes.Add(new ExposableXenotypeChance(xenoChance.xenotype, xenoChance.chance / totalWeight));
                    }
                }
            }
            else
            {
                tempXenotypes.Add(new ExposableXenotypeChance(XenotypeDefOf.Baseliner, 1.0f));
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

            var mainRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 95f);
            Widgets.DrawMenuSection(mainRect);

            var leftRect = new Rect(mainRect.x + 15, mainRect.y + 15, 300, mainRect.height - 30);
            var rightRect = new Rect(leftRect.xMax + 15, mainRect.y + 15, mainRect.width - leftRect.width - 45, mainRect.height - 30);
            if (ModsConfig.BiotechActive)
            {
                DrawXenotypeEditor(leftRect);
            }
            DrawDetailsEditor(rightRect);

            var bottomRect = new Rect(inRect.x, inRect.yMax - 35f, inRect.width, 35f);

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
            var headingRect = listing.GetRect(Text.LineHeight);
            Widgets.Label(
                headingRect,
                "WB_PopEditor_Xenotypes".Translate());
            var total = tempXenotypes.Sum(xenotype => xenotype.chance);
            var oldAnchor = Text.Anchor;
            var oldColor = GUI.color;
            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = Mathf.Approximately(total, 1f)
                ? Color.gray
                : Color.yellow;
            Widgets.Label(
                headingRect,
                "WB_PopEditor_XenotypesTotal".Translate(
                    PercentageNumber(total) + "%"));
            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            listing.Gap(4);

            var scrollViewRect = listing.GetRect(rect.height - 100f - 32f);
            const float rowHeight = 32f;
            const float iconSize = 24f;
            const float deleteSize = 24f;
            const float percentageWidth = 56f;
            const float percentageLabelWidth = 12f;
            float viewHeight = tempXenotypes.Count * rowHeight;
            var viewRect = new Rect(0, 0, scrollViewRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(scrollViewRect, ref scrollPosition, viewRect);
            for (int i = tempXenotypes.Count - 1; i >= 0; i--)
            {
                var xeno = tempXenotypes[i];
                var rowRect = new Rect(
                    0f,
                    i * rowHeight,
                    viewRect.width,
                    rowHeight - 2f);
                if (i % 2 == 1)
                {
                    Widgets.DrawLightHighlight(rowRect);
                }
                Widgets.DrawHighlightIfMouseover(rowRect);

                var iconRect = new Rect(
                    rowRect.x + 4f,
                    rowRect.y + 3f,
                    iconSize,
                    iconSize);
                Widgets.DrawTextureFitted(iconRect, xeno.xenotype.Icon, 1f);

                var deleteRect = new Rect(
                    rowRect.xMax - deleteSize - 4f,
                    rowRect.y + 3f,
                    deleteSize,
                    deleteSize);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    xenotypeChanceBuffers.Remove(xeno);
                    tempXenotypes.RemoveAt(i);
                    break;
                }

                var percentageLabelRect = new Rect(
                    deleteRect.x - percentageLabelWidth - 4f,
                    rowRect.y + 4f,
                    percentageLabelWidth,
                    22f);
                var percentageRect = new Rect(
                    percentageLabelRect.x - percentageWidth - 4f,
                    rowRect.y + 3f,
                    percentageWidth,
                    24f);
                var labelRect = new Rect(
                    iconRect.xMax + 6f,
                    rowRect.y + 4f,
                    Mathf.Max(0f, percentageRect.x - iconRect.xMax - 12f),
                    22f);
                Widgets.Label(
                    labelRect,
                    GenText.Truncate(
                        xeno.xenotype.LabelCap.ToString(),
                        labelRect.width));
                TooltipHandler.TipRegion(labelRect, xeno.xenotype.description);

                float percentage = xeno.chance * 100f;
                if (!xenotypeChanceBuffers.TryGetValue(
                        xeno,
                        out var percentageBuffer))
                {
                    percentageBuffer = PercentageNumber(xeno.chance);
                }
                Widgets.TextFieldNumeric(
                    percentageRect,
                    ref percentage,
                    ref percentageBuffer,
                    0f,
                    100f);
                xenotypeChanceBuffers[xeno] = percentageBuffer;
                xeno.chance = percentage / 100f;
                Widgets.Label(percentageLabelRect, "%");
            }
            Widgets.EndScrollView();

            var actionsRect = new Rect(
                rect.x,
                scrollViewRect.yMax + 6f,
                rect.width,
                30f);
            const float actionButtonGap = 6f;
            float actionButtonWidth = (actionsRect.width - actionButtonGap) / 2f;
            var balanceButtonRect = new Rect(
                actionsRect.x,
                actionsRect.y,
                actionButtonWidth,
                actionsRect.height);
            var addButtonRect = new Rect(
                balanceButtonRect.xMax + actionButtonGap,
                actionsRect.y,
                actionButtonWidth,
                actionsRect.height);
            if (Widgets.ButtonText(
                    balanceButtonRect,
                    "WB_PopEditor_BalanceTo100".Translate(),
                    active: tempXenotypes.Count > 0))
            {
                BalanceXenotypeChances();
            }

            if (Widgets.ButtonText(
                    addButtonRect,
                    "WB_PopEditor_AddNew".Translate()))
            {
                var presentation = new DefPickerPresentation<XenotypeDef>(
                    labelGetter: xenotype => xenotype.LabelCap.ToString(),
                    tooltipGetter: xenotype => xenotype.description,
                    iconGetter: xenotype => xenotype.Icon);
                Find.WindowStack.Add(new Window_DefPicker<XenotypeDef>(
                    "WB_PopEditor_Xenotypes".Translate(),
                    DefDatabase<XenotypeDef>.AllDefs.Where(
                        xenotype => tempXenotypes.All(
                            selected => selected.xenotype != xenotype)),
                    xenotype => tempXenotypes.Add(
                        new ExposableXenotypeChance(xenotype, 0f)),
                    "WB_DefPickerNoEntries".Translate(),
                    presentation));
            }

            var warningLabelRect = new Rect(
                rect.x,
                actionsRect.yMax + 8f,
                rect.width,
                36f);
            Widgets.Label(warningLabelRect, "WB_PopEditor_XenotypeSaveWarning".Translate());

            listing.End();
        }

        private void BalanceXenotypeChances()
        {
            if (tempXenotypes.Count == 0)
            {
                return;
            }

            const int totalUnits = 10000;
            var totalWeight = tempXenotypes.Sum(
                xenotype => Mathf.Max(0f, xenotype.chance));
            var units = new int[tempXenotypes.Count];
            var remainders = new float[tempXenotypes.Count];
            var allocatedUnits = 0;
            for (var i = 0; i < tempXenotypes.Count; i++)
            {
                var exactUnits = Mathf.Approximately(totalWeight, 0f)
                    ? (float)totalUnits / tempXenotypes.Count
                    : Mathf.Max(0f, tempXenotypes[i].chance) /
                      totalWeight * totalUnits;
                units[i] = Mathf.FloorToInt(exactUnits);
                remainders[i] = exactUnits - units[i];
                allocatedUnits += units[i];
            }

            foreach (var index in Enumerable.Range(
                         0,
                         tempXenotypes.Count)
                     .OrderByDescending(index => remainders[index])
                     .ThenBy(index => index)
                     .Take(totalUnits - allocatedUnits))
            {
                units[index]++;
            }

            for (var i = 0; i < tempXenotypes.Count; i++)
            {
                var xenotype = tempXenotypes[i];
                xenotype.chance = units[i] / (float)totalUnits;
                xenotypeChanceBuffers[xenotype] =
                    PercentageNumber(xenotype.chance);
            }
        }

        private static string PercentageNumber(float chance)
        {
            return (chance * 100f).ToString("0.##");
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

            if (ModsConfig.IdeologyActive)
            {
                if (listing.ButtonTextLabeled("WB_PopEditor_Ideoligion".Translate(), tempIdeo?.name ?? "None".Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(Find.IdeoManager.IdeosListForReading
                        .OrderBy(i => i.name)
                        .Select(i => new FloatMenuOption(i.name, () => tempIdeo = i))
                        .ToList()));
                }
            }

            listing.Label("WB_PopEditor_Relations".Translate() + ": " + tempGoodwill);
            tempGoodwill = (int)listing.Slider(tempGoodwill, -100, 100);

            listing.CheckboxLabeled("WB_PopEditor_PermanentEnemy".Translate(), ref tempPermanentEnemy);
            listing.Gap(6f);

            var originalColor = GUI.color;

            if (ModsConfig.IdeologyActive)
            {
                listing.CheckboxLabeled("WB_PopEditor_DisablePreferences".Translate(), ref tempDisableMemeRequirements);
                GUI.color = Color.gray;
                Text.Font = GameFont.Tiny;
                listing.Label("WB_PopEditor_DisablePreferencesDesc".Translate());
                Text.Font = GameFont.Small;
                GUI.color = originalColor;
                listing.Gap(6f);
            }

            if (ModsConfig.BiotechActive)
            {
                listing.CheckboxLabeled("WB_PopEditor_ForceXenotype".Translate(), ref tempForceXenotypeOverride);
                GUI.color = Color.gray;
                Text.Font = GameFont.Tiny;
                listing.Label("WB_PopEditor_ForceXenotypeDesc".Translate());
                Text.Font = GameFont.Small;
                GUI.color = originalColor;
            }

            listing.End();
        }

        private void Save()
        {
            var sum = tempXenotypes.Sum(x => x.chance);
            if (!Mathf.Approximately(sum, 1f))
            {
                Messages.Message("WB_XenotypeTotalError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            var popData = faction.GetPopulationData() ?? new FactionPopulationData();

            popData.pawnSingular = tempSingular;
            popData.pawnsPlural = tempPlural;
            popData.leaderTitle = tempLeaderTitle;
            popData.techLevel = tempTechLevel;
            popData.permanentEnemy = tempPermanentEnemy;
            popData.disableMemeRequirements = tempDisableMemeRequirements;
            popData.forceXenotypeOverride = tempForceXenotypeOverride;
            popData.xenotypeChances = tempXenotypes.Where(x => x.chance > 0.0001f).ToList();

            faction.SetPopulationData(popData);

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
