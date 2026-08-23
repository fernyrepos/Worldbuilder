using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal enum TileBrushToolKind
    {
        Terrain,
        Water,
        Plants,
        Trees,
        Mountains,
        Ores,
        Chunks,
        Snow,
        Pollution,
        VegetationMix
    }

    internal enum TileBrushOperation
    {
        Paint,
        Clear
    }

    internal enum TileBrushShape
    {
        Circle,
        Rugged,
        Rectangle
    }

    internal readonly struct BrushApplyResult
    {
        internal readonly bool Changed;
        internal readonly bool Irreversible;
        internal readonly bool RelocationFailed;

        internal BrushApplyResult(bool changed, bool irreversible = false, bool relocationFailed = false)
        {
            Changed = changed;
            Irreversible = irreversible;
            RelocationFailed = relocationFailed;
        }
    }

    internal sealed class TileBrushSettings
    {
        private readonly Dictionary<TileBrushToolKind, Def> selections =
            new Dictionary<TileBrushToolKind, Def>();
        private readonly List<ThingDef> vegetationMixSelections =
            new List<ThingDef>();

        internal TileBrushToolKind ToolKind = TileBrushToolKind.Terrain;
        internal TileBrushOperation Operation = TileBrushOperation.Paint;
        internal TileBrushShape Shape = TileBrushShape.Circle;
        internal bool EyedropperActive;
        internal bool FullReplacement;
        internal int Radius = 2;
        internal float TerrainDensity = 1f;
        internal float VegetationDensity = 0.35f;
        internal float OreDensity = 1f;
        internal float ChunkDensity = 0.2f;
        internal float PlantGrowth = 1f;
        internal float SnowDepth = 1f;

        internal ITileBrushTool Tool => TileBrushToolRegistry.Get(ToolKind);
        internal IReadOnlyList<ThingDef> VegetationMixSelections =>
            vegetationMixSelections;
        internal bool CanPaint =>
            ToolKind != TileBrushToolKind.VegetationMix ||
            vegetationMixSelections.Count > 0;

        internal Def SelectedDef
        {
            get
            {
                EnsureValidSelection();
                if (ToolKind == TileBrushToolKind.VegetationMix)
                {
                    return null;
                }

                selections.TryGetValue(ToolKind, out var selected);
                return selected;
            }
        }

        internal void SelectTool(TileBrushToolKind kind)
        {
            ToolKind = kind;
            EyedropperActive = false;
            if (!TileBrushToolRegistry.Get(kind).SupportsClear)
            {
                Operation = TileBrushOperation.Paint;
            }

            EnsureValidSelection();
        }

        internal void SetSelectedDef(Def def)
        {
            if (ToolKind == TileBrushToolKind.VegetationMix)
            {
                AddVegetationMixDef(def as ThingDef);
                return;
            }

            if (def != null && Tool.Options.Contains(def))
            {
                selections[ToolKind] = def;
                Operation = TileBrushOperation.Paint;
            }
        }

        internal bool IsVegetationMixSelected(Def def)
        {
            return def is ThingDef thingDef &&
                   vegetationMixSelections.Contains(thingDef);
        }

        internal void ToggleVegetationMixDef(Def def)
        {
            if (!(def is ThingDef thingDef) ||
                !TileBrushToolRegistry
                    .Get(TileBrushToolKind.VegetationMix)
                    .Options
                    .Contains(thingDef))
            {
                return;
            }

            Operation = TileBrushOperation.Paint;
            if (!vegetationMixSelections.Remove(thingDef))
            {
                AddVegetationMixDef(thingDef);
            }
        }

        internal void AddVegetationMixDef(ThingDef def)
        {
            if (def == null ||
                vegetationMixSelections.Contains(def) ||
                !TileBrushToolRegistry
                    .Get(TileBrushToolKind.VegetationMix)
                    .Options
                    .Contains(def))
            {
                return;
            }

            vegetationMixSelections.Add(def);
            vegetationMixSelections.Sort((left, right) =>
                string.CompareOrdinal(left.defName, right.defName));
            Operation = TileBrushOperation.Paint;
        }

        internal void ClearVegetationMix()
        {
            vegetationMixSelections.Clear();
        }

        internal float DensityForCurrentTool()
        {
            switch (ToolKind)
            {
                case TileBrushToolKind.Terrain:
                    return TerrainDensity;
                case TileBrushToolKind.Plants:
                case TileBrushToolKind.Trees:
                case TileBrushToolKind.VegetationMix:
                    return VegetationDensity;
                case TileBrushToolKind.Ores:
                    return OreDensity;
                case TileBrushToolKind.Chunks:
                    return ChunkDensity;
                default:
                    return 1f;
            }
        }

        private void EnsureValidSelection()
        {
            var options = Tool.Options;
            if (ToolKind == TileBrushToolKind.VegetationMix)
            {
                vegetationMixSelections.RemoveAll(def =>
                    def == null || !options.Contains(def));
                selections.Remove(ToolKind);
                return;
            }

            if (options.Count == 0)
            {
                selections.Remove(ToolKind);
                return;
            }

            if (!selections.TryGetValue(ToolKind, out var selected) || !options.Contains(selected))
            {
                selections[ToolKind] = options[0];
            }
        }
    }

    internal interface ITileBrushTool
    {
        TileBrushToolKind Kind { get; }
        string Label { get; }
        bool SupportsClear { get; }
        bool SupportsDensity { get; }
        IReadOnlyList<Def> Options { get; }

        BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell);

        bool Pick(TileBrushSettings settings, Map map, IntVec3 cell);
    }

    internal static class TileBrushToolRegistry
    {
        private static readonly Dictionary<TileBrushToolKind, ITileBrushTool> Tools =
            new Dictionary<TileBrushToolKind, ITileBrushTool>
            {
                { TileBrushToolKind.Terrain, new TerrainBrushTool(water: false) },
                { TileBrushToolKind.Water, new TerrainBrushTool(water: true) },
                { TileBrushToolKind.Plants, new PlantBrushTool(trees: false) },
                { TileBrushToolKind.Trees, new PlantBrushTool(trees: true) },
                { TileBrushToolKind.Mountains, new MountainBrushTool() },
                { TileBrushToolKind.Ores, new OreBrushTool() },
                { TileBrushToolKind.Chunks, new ChunkBrushTool() },
                { TileBrushToolKind.Snow, new SnowBrushTool() },
                { TileBrushToolKind.Pollution, new PollutionBrushTool() },
                {
                    TileBrushToolKind.VegetationMix,
                    new VegetationMixBrushTool()
                }
            };
        private static readonly TileBrushToolKind[] ToolOrder =
        {
            TileBrushToolKind.Terrain,
            TileBrushToolKind.Water,
            TileBrushToolKind.Plants,
            TileBrushToolKind.Trees,
            TileBrushToolKind.VegetationMix,
            TileBrushToolKind.Mountains,
            TileBrushToolKind.Ores,
            TileBrushToolKind.Chunks,
            TileBrushToolKind.Snow,
            TileBrushToolKind.Pollution
        };
        private static readonly IReadOnlyList<ITileBrushTool> OrderedTools =
            ToolOrder.Select(kind => Tools[kind]).ToList();

        internal static ITileBrushTool Get(TileBrushToolKind kind)
        {
            return Tools[kind];
        }

        internal static bool TryPickAtMouse(
            TileBrushSettings settings,
            Map map,
            IntVec3 cell)
        {
            var thingsUnderMouse = GenUI.ThingsUnderMouse(
                UI.MouseMapPosition(),
                0.8f,
                TargetingParameters.ForThing());
            if (settings.ToolKind == TileBrushToolKind.VegetationMix)
            {
                var vegetation = thingsUnderMouse
                    .OfType<Plant>()
                    .FirstOrDefault(plant =>
                        Get(TileBrushToolKind.VegetationMix)
                            .Options
                            .Contains(plant.def));
                if (vegetation != null)
                {
                    settings.AddVegetationMixDef(vegetation.def);
                    settings.PlantGrowth = vegetation.Growth;
                    settings.Operation = TileBrushOperation.Paint;
                    settings.EyedropperActive = false;
                    return true;
                }
            }

            if (thingsUnderMouse.Count > 0)
            {
                var thing = thingsUnderMouse[0];
                if (!TryGetToolKind(thing, out var thingToolKind))
                {
                    return false;
                }

                return SelectAndPick(
                    settings,
                    map,
                    thing.Spawned ? thing.Position : cell,
                    thingToolKind);
            }

            if (map.snowGrid.GetDepth(cell) > 0f)
            {
                return SelectAndPick(
                    settings,
                    map,
                    cell,
                    TileBrushToolKind.Snow);
            }

            if (ModsConfig.BiotechActive &&
                map.pollutionGrid.IsPolluted(cell))
            {
                return SelectAndPick(
                    settings,
                    map,
                    cell,
                    TileBrushToolKind.Pollution);
            }

            var terrain = map.terrainGrid.TerrainAt(cell);
            var terrainToolKind = terrain.IsWater
                ? TileBrushToolKind.Water
                : TileBrushToolKind.Terrain;
            if (!Get(terrainToolKind).Options.Contains(terrain))
            {
                return false;
            }

            return SelectAndPick(
                settings,
                map,
                cell,
                terrainToolKind);
        }

        private static bool TryGetToolKind(
            Thing thing,
            out TileBrushToolKind kind)
        {
            if (TileBrushToolBase.IsResourceMineable(thing?.def))
            {
                kind = TileBrushToolKind.Ores;
                return Get(kind).Options.Contains(thing.def);
            }

            if (thing?.def?.IsNonResourceNaturalRock == true)
            {
                kind = TileBrushToolKind.Mountains;
                return Get(kind).Options.Contains(thing.def);
            }

            if (TileBrushToolBase.IsStoneChunk(thing?.def))
            {
                kind = TileBrushToolKind.Chunks;
                return Get(kind).Options.Contains(thing.def);
            }

            if (thing is Plant plant && plant.def?.plant != null)
            {
                kind = plant.def.plant.IsTree
                    ? TileBrushToolKind.Trees
                    : TileBrushToolKind.Plants;
                return Get(kind).Options.Contains(plant.def);
            }

            kind = default;
            return false;
        }

        private static bool SelectAndPick(
            TileBrushSettings settings,
            Map map,
            IntVec3 cell,
            TileBrushToolKind kind)
        {
            var previousKind = settings.ToolKind;
            var previousOperation = settings.Operation;
            var previousEyedropperActive =
                settings.EyedropperActive;
            settings.SelectTool(kind);
            if (settings.Tool.Pick(settings, map, cell))
            {
                settings.Operation = TileBrushOperation.Paint;
                settings.EyedropperActive = false;
                return true;
            }

            settings.SelectTool(previousKind);
            settings.Operation = previousOperation;
            settings.EyedropperActive =
                previousEyedropperActive;
            return false;
        }

        internal static IReadOnlyList<ITileBrushTool> All => OrderedTools;
    }

    internal abstract class TileBrushToolBase : ITileBrushTool
    {
        public abstract TileBrushToolKind Kind { get; }
        public abstract string Label { get; }
        public virtual bool SupportsClear => true;
        public virtual bool SupportsDensity => false;
        public virtual IReadOnlyList<Def> Options => Array.Empty<Def>();

        public abstract BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell);

        public abstract bool Pick(TileBrushSettings settings, Map map, IntVec3 cell);

        internal static bool IsStoneChunk(ThingDef def)
        {
            return def?.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) == true;
        }

        internal static bool IsResourceMineable(ThingDef def)
        {
            return def?.category == ThingCategory.Building &&
                   def.mineable &&
                   def.building?.isResourceRock == true;
        }
    }

    internal sealed class TerrainBrushTool : TileBrushToolBase
    {
        private readonly bool water;
        private List<Def> options;

        internal TerrainBrushTool(bool water)
        {
            this.water = water;
        }

        public override TileBrushToolKind Kind =>
            water ? TileBrushToolKind.Water : TileBrushToolKind.Terrain;

        public override string Label =>
            water
                ? "WB_TileBrushToolWater".Translate()
                : "WB_TileBrushToolTerrain".Translate();

        public override bool SupportsDensity => !water;

        public override IReadOnlyList<Def> Options =>
            options ??= DefDatabase<TerrainDef>.AllDefsListForReading
                .Where(def =>
                    !def.defName.NullOrEmpty() &&
                    !def.label.NullOrEmpty() &&
                    (water
                        ? def.IsWater
                        : def.natural && !def.IsWater))
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName)
                .Cast<Def>()
                .ToList();

        public override BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell)
        {
            var terrain = settings.SelectedDef as TerrainDef;
            if (terrain == null)
            {
                return default;
            }

            if (!controller.PrepareTerrainPlacement(
                    settings,
                    cell,
                    out var preparationChanged,
                    out var irreversible,
                    out var relocationFailed))
            {
                return new BrushApplyResult(false, false, relocationFailed);
            }

            var changed = preparationChanged;
            if (controller.Map.terrainGrid.TerrainAt(cell) != terrain)
            {
                controller.SetTerrainPreservingPlants(cell, terrain);
                changed = true;
            }

            return new BrushApplyResult(changed, irreversible, relocationFailed);
        }

        public override bool Pick(TileBrushSettings settings, Map map, IntVec3 cell)
        {
            var terrain = map.terrainGrid.TerrainAt(cell);
            if (!Options.Contains(terrain))
            {
                return false;
            }

            settings.SetSelectedDef(terrain);
            return true;
        }
    }

    internal sealed class PlantBrushTool : TileBrushToolBase
    {
        private readonly bool trees;
        private List<Def> options;

        internal PlantBrushTool(bool trees)
        {
            this.trees = trees;
        }

        public override TileBrushToolKind Kind =>
            trees ? TileBrushToolKind.Trees : TileBrushToolKind.Plants;

        public override string Label =>
            trees ? "WB_TileBrushToolTrees".Translate() : "WB_TileBrushToolPlants".Translate();

        public override bool SupportsDensity => true;

        public override IReadOnlyList<Def> Options =>
            options ??= DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def =>
                    def.category == ThingCategory.Plant &&
                    def.plant != null &&
                    def.size.x == 1 &&
                    def.size.z == 1 &&
                    def.plant.IsTree == trees &&
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
            var plantDef = settings.SelectedDef as ThingDef;
            if (plantDef == null ||
                plantDef.size.x != 1 ||
                plantDef.size.z != 1)
            {
                return default;
            }

            if (!controller.PreparePlantPlacement(
                    settings,
                    plantDef,
                    cell,
                    out var preparationChanged,
                    out var irreversible,
                    out var relocationFailed))
            {
                return new BrushApplyResult(false, false, relocationFailed);
            }

            var plant = (Plant)ThingMaker.MakeThing(plantDef);
            plant.Growth = Mathf.Clamp01(settings.PlantGrowth);
            controller.CaptureBeforeCellChange(cell);
            GenSpawn.Spawn(plant, cell, controller.Map, WipeMode.Vanish);
            return new BrushApplyResult(true, irreversible, relocationFailed);
        }

        public override bool Pick(TileBrushSettings settings, Map map, IntVec3 cell)
        {
            var plant = cell.GetPlant(map);
            if (plant?.def?.plant?.IsTree != trees)
            {
                return false;
            }

            settings.SetSelectedDef(plant.def);
            settings.PlantGrowth = plant.Growth;
            return true;
        }
    }

    internal sealed class MountainBrushTool : TileBrushToolBase
    {
        private List<Def> options;

        public override TileBrushToolKind Kind => TileBrushToolKind.Mountains;
        public override string Label => "WB_TileBrushToolMountains".Translate();
        public override IReadOnlyList<Def> Options =>
            options ??= DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsNonResourceNaturalRock)
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName)
                .Cast<Def>()
                .ToList();

        public override BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell)
        {
            var rockDef = settings.SelectedDef as ThingDef;
            if (rockDef == null ||
                rockDef.size.x != 1 ||
                rockDef.size.z != 1)
            {
                return default;
            }

            if (!settings.FullReplacement &&
                controller.WouldWipePreservedThing(rockDef, cell))
            {
                return default;
            }

            if (!controller.PreparePlacement(
                    settings,
                    cell,
                    out var preparationChanged,
                    out var irreversible,
                    out var relocationFailed))
            {
                return new BrushApplyResult(false, false, relocationFailed);
            }

            controller.CaptureBeforeCellChange(cell);
            if (rockDef.building?.naturalTerrain != null &&
                controller.Map.terrainGrid.TerrainAt(cell) != rockDef.building.naturalTerrain)
            {
                controller.Map.terrainGrid.SetTerrain(cell, rockDef.building.naturalTerrain);
                preparationChanged = true;
            }

            if (cell.GetRoof(controller.Map) != RoofDefOf.RoofRockThick)
            {
                controller.Map.roofGrid.SetRoof(cell, RoofDefOf.RoofRockThick);
                preparationChanged = true;
            }

            var spawned = GenSpawn.Spawn(
                rockDef,
                cell,
                controller.Map,
                WipeMode.Vanish);
            if (spawned != null)
            {
                controller.MarkMountainCellForFog(cell);
            }

            return new BrushApplyResult(
                preparationChanged || spawned != null,
                irreversible,
                relocationFailed);
        }

        public override bool Pick(TileBrushSettings settings, Map map, IntVec3 cell)
        {
            var edifice = cell.GetEdifice(map);
            if (edifice?.def?.IsNonResourceNaturalRock != true)
            {
                return false;
            }

            settings.SetSelectedDef(edifice.def);
            return true;
        }
    }

    internal sealed class OreBrushTool : TileBrushToolBase
    {
        private List<Def> options;

        public override TileBrushToolKind Kind => TileBrushToolKind.Ores;
        public override string Label => "WB_TileBrushToolOres".Translate();
        public override bool SupportsDensity => true;
        public override IReadOnlyList<Def> Options =>
            options ??= DefDatabase<ThingDef>.AllDefsListForReading
                .Where(IsResourceMineable)
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName)
                .Cast<Def>()
                .ToList();

        public override BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell)
        {
            var oreDef = settings.SelectedDef as ThingDef;
            if (oreDef == null ||
                oreDef.size.x != 1 ||
                oreDef.size.z != 1)
            {
                return default;
            }

            if (!settings.FullReplacement &&
                controller.WouldWipePreservedThing(oreDef, cell))
            {
                return default;
            }

            if (!controller.PreparePlacement(
                    settings,
                    cell,
                    out var preparationChanged,
                    out var irreversible,
                    out var relocationFailed))
            {
                return new BrushApplyResult(false, false, relocationFailed);
            }

            controller.CaptureBeforeCellChange(cell);
            var spawned = GenSpawn.Spawn(
                oreDef,
                cell,
                controller.Map,
                WipeMode.Vanish);
            return new BrushApplyResult(
                preparationChanged || spawned != null,
                irreversible,
                relocationFailed);
        }

        public override bool Pick(TileBrushSettings settings, Map map, IntVec3 cell)
        {
            var edifice = cell.GetEdifice(map);
            if (!IsResourceMineable(edifice?.def))
            {
                return false;
            }

            settings.SetSelectedDef(edifice.def);
            return true;
        }
    }

    internal sealed class ChunkBrushTool : TileBrushToolBase
    {
        private List<Def> options;

        public override TileBrushToolKind Kind => TileBrushToolKind.Chunks;
        public override string Label => "WB_TileBrushToolChunks".Translate();
        public override bool SupportsDensity => true;
        public override IReadOnlyList<Def> Options =>
            options ??= DefDatabase<ThingDef>.AllDefsListForReading
                .Where(IsStoneChunk)
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName)
                .Cast<Def>()
                .ToList();

        public override BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell)
        {
            var chunkDef = settings.SelectedDef as ThingDef;
            if (chunkDef == null)
            {
                return default;
            }

            if (!controller.PreparePlacement(
                    settings,
                    cell,
                    out var preparationChanged,
                    out var irreversible,
                    out var relocationFailed))
            {
                return new BrushApplyResult(false, false, relocationFailed);
            }

            var chunk = ThingMaker.MakeThing(chunkDef);
            chunk.stackCount = 1;
            controller.CaptureBeforeCellChange(cell);
            GenSpawn.Spawn(chunk, cell, controller.Map, WipeMode.Vanish);
            return new BrushApplyResult(true, irreversible, relocationFailed);
        }

        public override bool Pick(TileBrushSettings settings, Map map, IntVec3 cell)
        {
            var chunk = map.thingGrid
                .ThingsListAt(cell)
                .FirstOrDefault(thing => IsStoneChunk(thing.def));
            if (chunk == null)
            {
                return false;
            }

            settings.SetSelectedDef(chunk.def);
            return true;
        }
    }

    internal sealed class SnowBrushTool : TileBrushToolBase
    {
        public override TileBrushToolKind Kind => TileBrushToolKind.Snow;
        public override string Label => "WB_TileBrushToolSnow".Translate();

        public override BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell)
        {
            var newDepth = Mathf.Clamp01(settings.SnowDepth);
            if (Mathf.Approximately(controller.Map.snowGrid.GetDepth(cell), newDepth))
            {
                return default;
            }

            controller.CaptureBeforeCellChange(cell);
            controller.Map.snowGrid.SetDepth(cell, newDepth);
            return new BrushApplyResult(true);
        }

        public override bool Pick(TileBrushSettings settings, Map map, IntVec3 cell)
        {
            settings.SnowDepth = map.snowGrid.GetDepth(cell);
            settings.Operation = settings.SnowDepth <= 0f
                ? TileBrushOperation.Clear
                : TileBrushOperation.Paint;
            return true;
        }
    }

    internal sealed class PollutionBrushTool : TileBrushToolBase
    {
        public override TileBrushToolKind Kind => TileBrushToolKind.Pollution;
        public override string Label => "WB_TileBrushToolPollution".Translate();

        public override BrushApplyResult Apply(
            TileBrushController controller,
            TileBrushSettings settings,
            IntVec3 cell)
        {
            if (!ModsConfig.BiotechActive)
            {
                return default;
            }

            const bool polluted = true;
            if (polluted)
            {
                var canPollute = settings.FullReplacement
                    ? controller.Map.terrainGrid.TerrainAt(cell).canBePolluted
                    : controller.Map.pollutionGrid.EverPollutable(cell);
                if (!canPollute)
                {
                    return default;
                }
            }

            if (!controller.PrepareNonDestructive(
                    settings,
                    cell,
                    out var preparationChanged,
                    out var irreversible,
                    out var relocationFailed))
            {
                return new BrushApplyResult(
                    false,
                    false,
                    relocationFailed);
            }

            if (controller.Map.pollutionGrid.IsPolluted(cell) == polluted)
            {
                return new BrushApplyResult(
                    preparationChanged,
                    irreversible,
                    relocationFailed);
            }

            controller.CaptureBeforeCellChange(cell);
            controller.Map.pollutionGrid.SetPolluted(cell, polluted);
            return new BrushApplyResult(true, irreversible, relocationFailed);
        }

        public override bool Pick(TileBrushSettings settings, Map map, IntVec3 cell)
        {
            if (!ModsConfig.BiotechActive)
            {
                return false;
            }

            settings.Operation = map.pollutionGrid.IsPolluted(cell)
                ? TileBrushOperation.Paint
                : TileBrushOperation.Clear;
            return true;
        }
    }
}
