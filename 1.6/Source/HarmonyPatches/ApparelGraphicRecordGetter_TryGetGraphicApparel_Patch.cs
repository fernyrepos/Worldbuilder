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
            if (data != null)
            {
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
                    var wornGraphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
                    rec = new ApparelGraphicRecord(wornGraphic, apparel);
                }
            }
        }
    }
}
