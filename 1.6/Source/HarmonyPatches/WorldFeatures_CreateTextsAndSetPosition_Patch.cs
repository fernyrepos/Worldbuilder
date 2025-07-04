using HarmonyLib;
using RimWorld.Planet;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using TMPro;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldFeatures), "CreateTextsAndSetPosition")]
    public static class WorldFeatures_CreateTextsAndSetPosition_Patch
    {

        private static FieldInfo textsField = AccessTools.Field(typeof(WorldFeatures), "texts");


        private static FieldInfo textMeshProField = AccessTools.Field(typeof(WorldFeatureTextMesh_TextMeshPro), "textMesh");
        private static FieldInfo legacyMeshField = AccessTools.Field(typeof(WorldFeatureTextMesh_Legacy), "mesh");
        private static FieldInfo legacyMaterialField = AccessTools.Field(typeof(WorldFeatureTextMesh_Legacy), "mat");

        public static void Postfix(WorldFeatures __instance)
        {
            var features = __instance.features;
            var textMeshes = textsField.GetValue(__instance) as List<WorldFeatureTextMesh>;

            if (features == null || textMeshes == null || features.Count != textMeshes.Count)
            {
                return;
            }

            for (int i = 0; i < features.Count; i++)
            {
                var feature = features[i];
                var textMesh = textMeshes[i];

                if (feature.def == DefDatabase<FeatureDef>.GetNamed("WB_MapLabelFeature"))
                {
                    string labelText = feature.name ?? "";

                    if (textMesh is WorldFeatureTextMesh_TextMeshPro tmpMesh)
                    {
                        var textMeshComponent = textMeshProField.GetValue(tmpMesh) as TextMeshPro;
                        if (textMeshComponent != null)
                        {
                            textMeshComponent.text = labelText;
                        }
                    }
                }
            }
        }
    }
}
