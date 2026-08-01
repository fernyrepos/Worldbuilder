using System;
using System.Collections.Generic;
using Verse;

namespace Worldbuilder
{
    public partial class Window_MapEditor
    {
        private static void OpenDefPicker<T>(
            TaggedString title,
            IEnumerable<T> defs,
            Action<T> onSelect) where T : Def
        {
            Find.WindowStack.Add(
                new Window_DefPicker<T>(
                    title,
                    defs,
                    onSelect,
                    "WB_DefPickerNoEntries".Translate()));
        }
    }
}
