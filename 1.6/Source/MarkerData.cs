namespace Worldbuilder
{
    public class MarkerData : WorldObjectData
    {
        public MarkerData Copy()
        {
            return new MarkerData
            {
                name = this.name,
                description = this.description,
                narrativeText = this.narrativeText,
                iconDef = this.iconDef,
                factionIconDef = this.factionIconDef,
                color = this.color
            };
        }
    }
}
