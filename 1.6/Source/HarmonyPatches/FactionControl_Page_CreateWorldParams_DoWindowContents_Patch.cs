using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder;

[HarmonyPatch]
public static class FactionControl_Page_CreateWorldParams_DoWindowContents_Patch
{
    private const string PackageId = "thereallemon.factioncontrol";
    private static MethodBase targetMethod;
    private static Type settingsWindowType;
    private static ConstructorInfo settingsWindowConstructor;

    public static bool Available { get; private set; }

    public static bool Prepare()
    {
        if (!ModsConfig.IsActive(PackageId))
        {
            return false;
        }

        targetMethod = AccessTools.Method(
            "FactionControl.Patch_Page_CreateWorldParams_DoWindowContents:Postfix");
        settingsWindowType = AccessTools.TypeByName("FactionControl.SettingsWindow");
        if (targetMethod == null || settingsWindowType == null ||
            !typeof(Window).IsAssignableFrom(settingsWindowType))
        {
            Log.Error("Worldbuilder: Faction Control compatibility could not find the expected planet generation UI methods.");
            return false;
        }

        settingsWindowConstructor = AccessTools.Constructor(settingsWindowType);
        if (settingsWindowConstructor == null)
        {
            Log.Error("Worldbuilder: Faction Control compatibility could not find the settings window constructor.");
            return false;
        }

        Available = true;
        return true;
    }

    public static MethodBase TargetMethod()
    {
        return targetMethod;
    }

    public static bool Prefix()
    {
        return WorldbuilderMod.settings?.enablePlanetGenOverhaul != true;
    }

    public static void DrawButton(Rect rect)
    {
        if (!Available || !Widgets.ButtonText(rect, "RFC.FactionControlName".Translate()))
        {
            return;
        }

        if (!Find.WindowStack.TryRemove(settingsWindowType, doCloseSound: true))
        {
            Find.WindowStack.Add((Window)settingsWindowConstructor.Invoke(Array.Empty<object>()));
        }
    }
}
