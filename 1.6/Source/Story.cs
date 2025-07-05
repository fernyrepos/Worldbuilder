using Verse;

namespace Worldbuilder
{
    public class Story : IExposable
    {
        public string title;
        public string text;
        public void ExposeData()
        {
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref text, "text");
        }
    }
}
