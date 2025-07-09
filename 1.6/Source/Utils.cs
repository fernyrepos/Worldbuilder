using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    public static class Utils
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

        public static List<T> GetSurfaceWorldObjects<T>() where T : WorldObject
        {
            var planetLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            return Find.World.worldObjects.worldObjects.OfType<T>().Where(x => x.Tile.Layer == planetLayer && x.Faction?.def?.isPlayer is false).ToList();
        }

        public static void LogMessage(this Thing thing, string message)
        {
            //if (Find.Selector.SelectedObjects.Contains(thing))
            //{
            //    Log.Message(thing + " - " + message);
            //    Log.ResetMessageCount();
            //}
        }

        public static void LogMessage(this ThingDef def, string message)
        {
            //if (def == ThingDefOf.Table1x2c)
            //{
            //    Log.Message(def + " - " + message);
            //    Log.ResetMessageCount();
            //}
        }
    }
}
