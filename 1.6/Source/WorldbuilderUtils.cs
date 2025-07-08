using System.Reflection;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    public static class WorldbuilderUtils
    {
        public static T Clone<T>(this T obj) where T : class
        {
            if (obj == null) return null;
            var inst = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            if (inst != null)
            {
                return (T)inst.Invoke(obj, null);
            }
            else
            {


                return obj;
            }
        }
        
        public static void LogMessage(this Thing thing, string message)
        {
            if (Find.Selector.SelectedObjects.Contains(thing))
            {
                Log.Message(thing + " - " + message);
                Log.ResetMessageCount();
            }
        }

        public static void LogMessage(this ThingDef def, string message)
        {
            if (def == ThingDefOf.Table1x2c)
            {
                Log.Message(def + " - " + message);
                Log.ResetMessageCount();
            }
        }
    }
}
