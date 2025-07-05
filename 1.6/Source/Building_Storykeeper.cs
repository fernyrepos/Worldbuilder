using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace Worldbuilder
{
    public class Building_Storykeeper : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "WB_OpenStoryLibrary".Translate(),
                defaultDesc = "WB_OpenStoryLibraryDesc".Translate(),
                icon = GizmoUtility.NarrativeGizmoIcon,
                action = () => Find.WindowStack.Add(new Window_StoryLibrary())
            };
        }
    }
}
