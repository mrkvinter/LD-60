using UnityEngine;

namespace Game.Utilities.Extensions
{
    public static class TransformExtensions
    {
        public static string GetHierarchyPath(this Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }
    }
}
