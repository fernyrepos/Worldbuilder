using System.Reflection;

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
    }
}