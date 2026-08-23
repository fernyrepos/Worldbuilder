using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
    public static class ApparelGraphicRecordGetter_TryGetGraphicApparel_Patch
    {
        public static void Postfix(bool __result, Apparel apparel, BodyTypeDef bodyType, bool forStatue, ref ApparelGraphicRecord rec)
        {
            if (__result is false) return;
            var data = apparel.GetCustomizationData();
            if (data is null) return;

            Color color = data.color ?? apparel.DrawColor;
            var styleDef = data.styleDef;
            if (styleDef?.wornGraphicPath.NullOrEmpty() is false)
            {
                var wornPath = styleDef.wornGraphicPath;
                string path = ((apparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead && apparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover && !apparel.RenderAsPack() && !(wornPath == BaseContent.PlaceholderImagePath) && !(wornPath == BaseContent.PlaceholderGearImagePath)) ? (wornPath + "_" + bodyType.defName) : wornPath);
                Shader shader = ShaderDatabase.Cutout;
                if (!forStatue)
                {
                    if (styleDef?.graphicData.shaderType != null)
                    {
                        shader = styleDef.graphicData.shaderType.Shader;
                    }
                    else if ((styleDef == null && apparel.def.apparel.useWornGraphicMask) || (styleDef != null && styleDef.UseWornGraphicMask))
                    {
                        shader = ShaderDatabase.CutoutComplex;
                    }
                }
                var wornGraphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, color);
                rec = new ApparelGraphicRecord(wornGraphic, apparel);
            }
            else if (data.color.HasValue && rec.graphic != null && rec.graphic.color != color)
            {
                rec = new ApparelGraphicRecord(rec.graphic.GetColoredVersion(rec.graphic.Shader, color, rec.graphic.colorTwo), rec.sourceApparel);
            }
        }
    }
}
