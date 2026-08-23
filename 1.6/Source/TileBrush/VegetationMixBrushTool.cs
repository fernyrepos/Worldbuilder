using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal sealed class VegetationMixBrushTool : TileBrushToolBase
    {
        private const uint SelectionSalt = 0x9E3779B9u;

        private List<Def> options;

        public override TileBrushToolKind Kind =>
            TileBrushToolKind.VegetationMix;

        public override string Label =>
            "WB_TileBrushToolVegetationMix".Translate();

        public override bool SupportsDensity => true;

        public override IReadOnlyList<Def> Options =>
            options ??= DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def =>
                    def.category == ThingCategory.Plant &&
                    def.plant != null &&
                    def.size.x == 1 &&
                    def.size.z == 1 &&
                    !def.label.NullOrEmpty())
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName)
                .Cast<Def>()
                .ToList();

        public override BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell)
        {
            var selected = settings.VegetationMixSelections;
            if (selected.Count == 0)
            {
                return default;
            }

            var startIndex = controller.DeterministicChoiceIndex(
                cell,
                selected.Count,
                SelectionSalt);
            for (var offset = 0; offset < selected.Count; offset++)
            {
                var plantDef = selected[(startIndex + offset) % selected.Count];
                if (plantDef == null ||
                    plantDef.size.x != 1 ||
                    plantDef.size.z != 1)
                {
                    continue;
                }

                if (!controller.PreparePlantPlacement(
                        settings,
                        plantDef,
                        cell,
                        out var preparationChanged,
                        out var irreversible,
                        out var relocationFailed))
                {
                    if (relocationFailed)
                    {
                        return new BrushApplyResult(false, false, true);
                    }

                    continue;
                }

                var plant = (Plant)ThingMaker.MakeThing(plantDef);
                plant.Growth = Mathf.Clamp01(settings.PlantGrowth);
                controller.CaptureBeforeCellChange(cell);
                GenSpawn.Spawn(plant, cell, controller.Map, WipeMode.Vanish);
                return new BrushApplyResult(
                    true,
                    irreversible,
                    relocationFailed);
            }

            return default;
        }

        public override bool Pick(
            TileBrushSettings settings,
            Map map,
            IntVec3 cell)
        {
            var plant = cell.GetPlant(map);
            if (plant == null || !Options.Contains(plant.def))
            {
                return false;
            }

            settings.AddVegetationMixDef(plant.def);
            settings.PlantGrowth = plant.Growth;
            return true;
        }
    }
}
