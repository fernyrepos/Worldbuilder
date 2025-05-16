using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(GenLabel), "ThingLabel", new Type[] { typeof(Thing), typeof(int), typeof(bool), typeof(bool) })]
    public static class GenLabel_ThingLabel_Patch
    {
        public static bool Prefix(ref string __result, Thing t, int stackCount, bool includeHp = true, bool includeQuality = true)
        {
            if (t != null && t.GetCustomizationData() is CustomizationData customizationData && customizationData != null && !customizationData.labelOverride.NullOrEmpty())
            {
                var text = customizationData.labelOverride;
                if (customizationData.includeMaterialInLabel && t.Stuff != null)
                {
                    text = "ThingMadeOfStuffLabel".Translate(t.Stuff.LabelAsStuff, text);
                }
                if (customizationData.includeAdditionalDetails)
                {
                    text += GenLabel.LabelExtras(t, includeHp, includeQuality);
                }
                if (stackCount > 1)
                {
                    text = text + " x" + stackCount.ToStringCached();
                }
                __result = text;
                return false;
            }
            return true;
        }
    }
}
