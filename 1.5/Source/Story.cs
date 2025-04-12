using Verse;

namespace Worldbuilder
{
    public class Story : IExposable
    {
        public string Title = "";
        public string Content = "";
        public int ID;

        public Story()
        {
            ID = Rand.Int;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Title, "title");
            Scribe_Values.Look(ref Content, "content");
            Scribe_Values.Look(ref ID, "ID");
        }
    }
}