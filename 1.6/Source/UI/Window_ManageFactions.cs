using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Worldbuilder
{
    [StaticConstructorOnStartup]
    [HotSwappable]
    public class Window_ManageFactions : Window
    {
        private Vector2 scrollPosition;
        private Faction factionForSettlementCreation;
        private Faction selectedFaction;
        public override Vector2 InitialSize => new Vector2(700f, 550f);
        public Window_ManageFactions()
        {
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            var title = "WB_ManageFactionsTitle".Translate();

            var titleRect = new Rect(inRect.x + 17f, inRect.y, inRect.width - 34f, 32f);
            Widgets.Label(titleRect, title);

            var titleSize = Text.CalcSize(title);

            var addButtonRect = new Rect(titleRect.x + titleSize.x + 8f, titleRect.y + 2f, 24f, 24f);

            if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
            {
                ShowAddFactionMenu();
            }

            float contentTop = titleRect.yMax + 10f;
            float listAreaHeight = inRect.height - contentTop - 10f;

            float leftColumnWidth = 380f; 
            float rightColumnWidth = inRect.width - leftColumnWidth - 30f;

            var leftColumnRect = new Rect(inRect.x + 10f, contentTop, leftColumnWidth, listAreaHeight);
            var rightColumnRect = new Rect(inRect.x + leftColumnWidth + 20f, contentTop, rightColumnWidth, listAreaHeight);

            DrawFactionList(leftColumnRect);
            DrawActionButtons(rightColumnRect);
        }

        private void DrawFactionList(Rect rect)
        {
            Widgets.DrawMenuSection(rect); 
            rect = rect.ContractedBy(1f);

            var visibleFactions = Find.FactionManager.AllFactionsListForReading
                .Where(f => !f.def.hidden && !f.def.isPlayer)
                .ToList();

            float rowHeight = 35f;
            float totalHeight = visibleFactions.Count * rowHeight;
            var viewRect = new Rect(0f, 0f, rect.width - 16f, totalHeight);

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            float currentY = 0f;

            foreach (var faction in visibleFactions)
            {
                Rect entryRect = new Rect(0f, currentY, viewRect.width, rowHeight);

                if (selectedFaction == faction)
                    Widgets.DrawHighlightSelected(entryRect);
                else if (Mouse.IsOver(entryRect))
                    Widgets.DrawHighlight(entryRect);

                Rect iconRect = new Rect(entryRect.x + 5f, entryRect.y + 5f, 24f, 24f);
                GUI.color = faction.Color;
                Widgets.DrawTextureFitted(iconRect, faction.def.FactionIcon, 1f);
                GUI.color = Color.white;

                Rect nameRect = new Rect(iconRect.xMax + 10f, entryRect.y, entryRect.width - 70f, entryRect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;
                Widgets.Label(nameRect, faction.Name);
                Text.Anchor = TextAnchor.UpperLeft;

                Rect trashRect = new Rect(entryRect.xMax - 30f, entryRect.y + 7f, 20f, 20f);

                if (Widgets.ButtonImage(trashRect, TexButton.Delete))
                {
                    ShowRemoveFactionConfirmation(faction);
                }

                if (Widgets.ButtonInvisible(entryRect))
                {
                    selectedFaction = faction;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }

                currentY += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawActionButtons(Rect rect)
        {
            if (selectedFaction == null) return;

            float btnHeight = 34f;
            float spacing = 15f;
            float currentY = rect.y;

            var btnRect1 = new Rect(rect.x, currentY, rect.width, btnHeight);
            if (Widgets.ButtonText(btnRect1, "WB_CreateNewSettlement".Translate()))
            {
                factionForSettlementCreation = selectedFaction;
                Close();
                Find.WorldTargeter.BeginTargeting(CreateSettlementAction, true);
            }
            currentY += btnHeight + spacing;

            var btnRect2 = new Rect(rect.x, currentY, rect.width, btnHeight);
            if (Widgets.ButtonText(btnRect2, "WB_EditPopulation".Translate()))
            {
                Find.WindowStack.Add(new Window_PopulationEditor(selectedFaction));
            }
            currentY += btnHeight + spacing;

            var btnRect3 = new Rect(rect.x, currentY, rect.width, btnHeight);
            if (Widgets.ButtonText(btnRect3, "WB_Customize".Translate()))
            {
                Find.WindowStack.Add(new Window_FactionCustomization(selectedFaction));
            }
        }

        private void ShowAddFactionMenu()
        {
            var options = new List<FloatMenuOption>();
            foreach (var factionDef in DefDatabase<FactionDef>.AllDefs.Where(d => !d.hidden && !d.isPlayer))
            {
                options.Add(new FloatMenuOption(factionDef.LabelCap, () => 
                    Find.WindowStack.Add(new Window_AddFaction(factionDef, SpawnFactionCallback))
                ));
            }

            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private void SpawnFactionCallback(FactionDef factionDef, int settlementCount, int minDistance)
        {
            Messages.Message("WB_ManageFactionsFactionAdded".Translate(factionDef.LabelCap), MessageTypeDefOf.PositiveEvent);
        }

        private void ShowRemoveFactionConfirmation(Faction faction)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "WB_RemoveFactionWarning".Translate(faction.Name), 
                () => RemoveFaction(faction), 
                true,
                "WB_RemoveFactionWarningTitle".Translate()
            ));
        }

        private void RemoveFaction(Faction faction)
        {
            var settlements = Find.WorldObjects.Settlements.Where(s => s.Faction == faction).ToList();
            foreach (var s in settlements) Find.WorldObjects.Remove(s);

            faction.RemoveAllRelations();
            Find.FactionManager.AllFactionsListForReading.Remove(faction);

            var allPawns = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead;
            for (int i = 0; i < allPawns.Count; i++)
            {
                if (allPawns[i].Faction == faction)
                {
                    allPawns[i].SetFaction(null);
                }
            }

            for (int j = 0; j < Find.Maps.Count; j++)
            {
                Find.Maps[j].pawnDestinationReservationManager.Notify_FactionRemoved(faction);
                Find.Maps[j].listerBuildings.Notify_FactionRemoved(faction);
            }

            Find.LetterStack.Notify_FactionRemoved(faction);
            Find.PlayLog.Notify_FactionRemoved(faction);
            Find.QuestManager.Notify_FactionRemoved(faction);
            Find.IdeoManager.Notify_FactionRemoved(faction);
            Find.TaleManager.Notify_FactionRemoved(faction);

            foreach (var map in Find.Maps)
            {
                map.events.Notify_FactionRemoved(faction);
            }

            if (selectedFaction == faction) selectedFaction = null;
            Messages.Message("WB_FactionRemovedSuccess".Translate(faction.Name), MessageTypeDefOf.PositiveEvent);
        }

        private bool CreateSettlementAction(GlobalTargetInfo target)
        {
            if (factionForSettlementCreation == null || !target.IsValid) return false;

            var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            settlement.SetFaction(factionForSettlementCreation);
            settlement.Tile = target.Tile;
            settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
            Find.WorldObjects.Add(settlement);

            Messages.Message("WB_ManageFactionsSettlementCreated".Translate(settlement.Name, factionForSettlementCreation.Name), MessageTypeDefOf.PositiveEvent);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();

            Find.WindowStack.Add(new Window_SettlementCustomization(settlement));

            factionForSettlementCreation = null;
            return true;
        }
    }
}
