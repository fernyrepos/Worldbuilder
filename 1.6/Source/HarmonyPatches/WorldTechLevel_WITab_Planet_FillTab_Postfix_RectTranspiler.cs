using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public static class WorldTechLevel_WITab_Planet_FillTab_Postfix_RectTranspiler
    {
        public static bool Prepare()
        {
            return ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId);
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("WorldTechLevel.Patches.Patch_WITab_Planet");
            return AccessTools.Method(type, "FillTab_Postfix");
        }

        private static Rect AdjustRect(Rect rect)
        {
            return new Rect(rect.x - 4f, rect.y + 4f, rect.width - 50, rect.height);    
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (codes[i].opcode == OpCodes.Ldloc_0)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WorldTechLevel_WITab_Planet_FillTab_Postfix_RectTranspiler), nameof(AdjustRect)));
                }
            }
        }
    }
}
