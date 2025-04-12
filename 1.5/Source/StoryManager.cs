using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    public class StoryManager : GameComponent
    {
        private List<Story> stories = new List<Story>();

        public StoryManager(Game game) : base()
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref stories, "stories", LookMode.Deep, new object[0]);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (stories == null)
                {
                    stories = new List<Story>();
                }
            }
        }

        public List<Story> GetAllStories()
        {
            return stories;
        }

        public void AddOrUpdateStory(Story story)
        {
            var existingStory = stories.FirstOrDefault(s => s.ID == story.ID);
            if (existingStory != null)
            {
                existingStory.Title = story.Title;
                existingStory.Content = story.Content;
            }
            else
            {
                stories.Add(story);
            }
        }

        public void RemoveStory(Story story)
        {
            stories.Remove(story);
        }
    }
}