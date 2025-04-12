using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [StaticConstructorOnStartup]
    public class WorldObject_MapMarker : WorldObject
    {
        private static new MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        public override Material Material
        {
            get
            {
                var markerData = MarkerDataManager.GetData(this);
                Texture2D iconTex = markerData?.IconDef?.Icon;

                if (iconTex != null)
                {
                    return MaterialPool.MatFrom(iconTex, base.Material.shader, Color.white, base.Material.renderQueue);
                }
                return base.Material;
            }
        }
        public override void Draw()
        {
            var markerData = MarkerDataManager.GetData(this);
            Color color = markerData?.color ?? Color.white;
            Material material = this.Material;
            float averageTileSize = Find.WorldGrid.averageTileSize;
            float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;
            if (this.def.expandingIcon && transitionPct > 0f)
            {
                float alpha = 1f - transitionPct;
                propertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(color.r, color.g, color.b, color.a * alpha));
                WorldRendererUtility.DrawQuadTangentialToPlanet(this.DrawPos, 0.7f * averageTileSize, 0.015f, material, counterClockwise: false, useSkyboxLayer: false, propertyBlock);
            }
            else
            {
                Material coloredMat = MaterialPool.MatFrom((Texture2D)material.mainTexture, material.shader, color, material.renderQueue);
                WorldRendererUtility.DrawQuadTangentialToPlanet(this.DrawPos, 0.7f * averageTileSize, 0.015f, coloredMat);
            }
        }
    }
}