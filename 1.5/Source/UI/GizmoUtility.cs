using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Worldbuilder
{
    [StaticConstructorOnStartup]
    public static class GizmoUtility
    {
        public static readonly Texture2D CustomizeGizmoIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/CustomizeIcon");
        public static readonly Texture2D NarrativeGizmoIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/NarrativeIcon");
        public static readonly Texture2D EraseGizmoIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Dismiss", true);

        public static bool TryCreateNarrativeGizmo(object target, out Command_Action narrativeGizmo)
        {
            narrativeGizmo = null;
            string narrativeText = null;

            if (target is Thing thing)
            {
                narrativeText = thing.GetCustomizationData()?.narrativeText;
            }
            else if (target is WorldObject worldObject && worldObject.def == WorldbuilderDefOf.Worldbuilder_MapMarker)
            {
                narrativeText = MarkerDataManager.GetData(worldObject)?.narrativeText;
            }

            if (!string.IsNullOrEmpty(narrativeText))
            {
                string textForWindow = narrativeText;
                narrativeGizmo = new Command_Action
                {
                    defaultLabel = "WB_CustomizeViewNarrative".Translate(),
                    defaultDesc = "WB_CustomizeViewNarrativeDesc".Translate(),
                    icon = NarrativeGizmoIcon,
                    action = () => { Find.WindowStack.Add(new NarrativeWindow(textForWindow)); }
                };
                return true;
            }

            return false;
        }
    }
}