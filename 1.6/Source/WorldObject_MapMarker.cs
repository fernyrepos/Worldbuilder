using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class WorldObject_MapMarker : WorldObject
    {
        private static new MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        public override Material Material
        {
            get
            {
                var markerData = MarkerData;
                var icon = markerData.GetIcon();
                if (icon != null)
                {
                    return MaterialPool.MatFrom(icon, base.Material.shader, markerData.color ?? Color.white, base.Material.renderQueue);
                }
                return base.Material;
            }
        }

        public override void Draw()
        {
            float averageTileSize = Tile.Layer.AverageTileSize;
            float rawTransitionPct = ExpandableWorldObjectsUtility.RawTransitionPct;
            if (!Tile.LayerDef.isSpace && (bool)Material)
            {
                if (def.expandingIcon && rawTransitionPct > 0f && !ExpandableWorldObjectsUtility.HiddenByRules(this))
                {
                    Color color = Material.color;
                    float num = 1f - rawTransitionPct;
                    propertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(color.r, color.g, color.b, color.a * num));
                    WorldRendererUtility.DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, DrawAltitude, Material, -90f, counterClockwise: false, useSkyboxLayer: false, propertyBlock);
                }
                else
                {
                    WorldRendererUtility.DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, DrawAltitude, Material, -90f);
                }
            }
        }

        public override string Label => MarkerData?.name ?? base.Label;
        
        public override string GetInspectString()
        {
            return MarkerData.description;
        }

        public override Texture2D ExpandingIcon
        {
            get
            {
                var markerData = MarkerData;
                if (markerData == null) return base.ExpandingIcon;
                return markerData.GetIcon();
            }
        }

        public MarkerData MarkerData => MarkerDataManager.GetData(this);
        //public override void Draw()
        //{
        //    var markerData = MarkerData;
        //    Color color = markerData?.color ?? Color.white;
        //    Material material = this.Material;
        //    float averageTileSize = Find.WorldGrid.AverageTileSize;
        //    float transitionPct = ExpandableWorldObjectsUtility.TransitionPct(this);
        //    if (this.def.expandingIcon && transitionPct > 0f)
        //    {
        //        float alpha = 1f - transitionPct;
        //        propertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(color.r, color.g, color.b, color.a * //alpha));
        //        WorldRendererUtility.DrawQuadTangentialToPlanet(this.DrawPos, 0.7f * averageTileSize, 0.015f, //material, counterClockwise: false, useSkyboxLayer: false, propertyBlock: propertyBlock);
        //    }
        //    else
        //    {
        //        Material coloredMat = MaterialPool.MatFrom((Texture2D)material.mainTexture, material.shader, color, //material.renderQueue);
        //        WorldRendererUtility.DrawQuadTangentialToPlanet(this.DrawPos, 0.7f * averageTileSize, 0.015f, //coloredMat);
        //    }
        //}

        public override void ExposeData()
        {
            base.ExposeData();
            var markerData = MarkerData;
            Scribe_Deep.Look(ref markerData, "markerData");
            Log.Message($"WorldObject_MapMarker: ExposeData for {this.Label} - " + markerData.GetIcon());
            MarkerDataManager.SetData(this, markerData);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
            Command_Action customizeMarkerGizmo = new Command_Action
            {
                defaultLabel = "WB_GizmoCustomizeLabel".Translate(),
                defaultDesc = "WB_GizmoCustomizeMarkerDesc".Translate(),
                icon = GizmoUtility.CustomizeGizmoIcon,
                action = () => Find.WindowStack.Add(new Window_MapMarkerCustomization(this))
            };
            yield return customizeMarkerGizmo;
            Command_Action eraseMarkerGizmo = new Command_Action
            {
                defaultLabel = "WB_GizmoEraseMarkerLabel".Translate(),
                defaultDesc = "WB_GizmoEraseMarkerDesc".Translate(),
                icon = GizmoUtility.EraseGizmoIcon,
                action = () =>
                {
                    MarkerDataManager.RemoveData(this);
                    this.Destroy();
                    Messages.Message("WB_MarkerErasedMessage".Translate(), MessageTypeDefOf.PositiveEvent);
                }
            };
            yield return eraseMarkerGizmo;
            if (GizmoUtility.TryCreateNarrativeGizmo(this, out var narrativeGizmo))
            {
                yield return narrativeGizmo;
            }
        }
    }
}
