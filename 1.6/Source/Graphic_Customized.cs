using System;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    public class Graphic_Customized : Graphic
    {
        [ThreadStatic]
        public static CustomizationData currentPrintingData;

        public Graphic subGraphic;
        public CustomizationData customData;

        public Graphic_Customized(Graphic subGraphic, CustomizationData data)
        {
            this.subGraphic = subGraphic;
            this.customData = data;
            this.color = subGraphic.color;
            this.drawSize = subGraphic.drawSize;
            this.path = subGraphic.path;
            this.data = subGraphic.data;
        }

        public override Material MatSingle => subGraphic.MatSingle;
        public override Material MatWest => subGraphic.MatWest;
        public override Material MatSouth => subGraphic.MatSouth;
        public override Material MatEast => subGraphic.MatEast;
        public override Material MatNorth => subGraphic.MatNorth;

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return new Graphic_Customized(subGraphic.GetColoredVersion(newShader, newColor, newColorTwo), customData);
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (subGraphic == null) return;

            if (customData.altitudeLayer.HasValue)
            {
                loc.y = Altitudes.AltitudeFor(customData.altitudeLayer.Value);
                if (thing != null) loc.y += thing.thingIDNumber * 0.0001f;
            }

            float totalAngle = extraRotation + customData.rotation;

            var finalOffset = new Vector3(customData.drawOffset.x, 0, customData.drawOffset.y);
            loc += finalOffset;

            subGraphic.DrawWorker(loc, rot, thingDef, thing, totalAngle);
        }

        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            currentPrintingData = customData;
            float totalRotation = extraRotation + customData.rotation;
            subGraphic.Print(layer, thing, totalRotation);
            currentPrintingData = null;
        }
    }
}
