using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [StaticConstructorOnStartup]
    public static class GizmoUtility
    {
        public static readonly Texture2D CustomizeGizmoIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/Gizmos/CustomizeIcon");
        public static readonly Texture2D NarrativeGizmoIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/Gizmos/NarrativeIcon");
        public static readonly Texture2D EraseGizmoIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/Gizmos/Erase", true);
        public static readonly Texture2D AddMarkerGizmoIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/Gizmos/AddMarker", true);
        public static readonly Texture2D ReadIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/Read");
        public static readonly Texture2D EditIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/Edit");
        public static readonly Texture2D CustomizationToggle = ContentFinder<Texture2D>.Get("Worldbuilder/UI/Gizmos/CustomizationToggle");
        public static bool TryCreateNarrativeGizmo(object target, out Command_Action narrativeGizmo)
        {
            narrativeGizmo = null;
            string narrativeText = null;
            string title = null;
            if (target is Thing thing)
            {
                narrativeText = thing.GetCustomizationData()?.narrativeText;
                title = thing.LabelCap;
            }
            else if (target is Settlement settlement)
            {
                var customData = SettlementCustomDataManager.GetData(settlement);
                narrativeText = customData?.narrativeText;
                title = settlement.Name;
            }
            else if (target is WorldObject_MapMarker mapMarker)
            {
                narrativeText = mapMarker.MarkerData.narrativeText;
                title = mapMarker.Label;
            }

            if (!string.IsNullOrEmpty(narrativeText))
            {
                string textForWindow = narrativeText;
                narrativeGizmo = new Command_Action
                {
                    defaultLabel = "WB_CustomizeViewNarrative".Translate(),
                    defaultDesc = "WB_CustomizeViewNarrativeDesc".Translate(),
                    icon = NarrativeGizmoIcon,
                    action = () => { Find.WindowStack.Add(new NarrativeWindow(title, textForWindow)); }
                };
                return true;
            }

            return false;
        }
    }
}
