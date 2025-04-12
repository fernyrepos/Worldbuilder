using Verse;
using RimWorld;

namespace Worldbuilder
{
    public class CompAbilityEffect_OpenStoryLibrary : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Find.WindowStack.Add(new Window_StoryLibrary());
        }
    }
}