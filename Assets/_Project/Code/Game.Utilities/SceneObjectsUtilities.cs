using System.Collections.Generic;
using UnityEngine;

namespace Game.Utilities
{
    public static class SceneObjectsUtilities
    {
        public static T RecursiveFindObject<T>(ICollection<GameObject> rootObjects) where T : Component
        {
            foreach (var rootObject in rootObjects)
            {
                var foundObject = rootObject.GetComponentInChildren<T>();
                if (foundObject != null)
                {
                    return foundObject;
                }
            }

            return null;
        }
    }
}