using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    /// <summary>
    /// Vanilla Expanded Factions: Medieval 2 seems to call a ColorChanged for the graphics of everything on the map after you change heraldry.
    /// I.e. ANY interaction with the heraldry editing menu calls ColorChanged for everything on the map. 
    ///
    /// <para/>
    /// This patch simply reapplies the graphics set via Worldbuilder afterwards.
    /// It'll also be called after any other mod does Notify_ColorChanged, of course, not just VFE: Medieval 2.
    /// 
    /// <para/>
    /// As we're only setting the graphics for the set of buildings the user edited manually it should be compatible with everything else.
    /// However, I can't foresee all cases; I think it's technically possible one mod's customization might overwrite another's.
    /// 
    /// <para/>
    /// <see
    ///     href="https://github.com/Vanilla-Expanded/VanillaFactionsExpanded-Medieval2/blob/438742256f54a965dc46da5f38a9ccfa5d4e19c9/1.6/Source/VFEMedieval/Heraldics/EditHeraldic.cs#L447-L471">
    ///      Relevant VFE code.
    /// </see>
    /// </summary>
    [HotSwappable]
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.Notify_ColorChanged))]
    [HarmonyPriority(Priority.Last)]
    public static class ThingWithComps_Notify_ColorChanged_Patch
    {
        public static void Postfix(ThingWithComps __instance)
        {
            if (__instance is Pawn) return;

            var customizationData = __instance.GetCustomizationData();
            if (customizationData == null) return;

            var customGraphic = customizationData.GetGraphic(__instance);
            if (customGraphic == null) return;

            __instance.graphicInt = customGraphic;
            __instance.styleGraphicInt = customGraphic;
        }
    }
}
