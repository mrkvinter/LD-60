using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Utilities
{
    public class PositionRetargeter : MonoBehaviour
    {
        [SerializeField] Transform targetRoot;
        
        [Button]
        public void Retarget()
        {
            Retarget(transform, targetRoot);
        }
        
        private void Retarget(Transform root, Transform target)
        {
            var queue = new Queue<(Transform, Transform)>();
            queue.Enqueue((root, target));
            while (queue.Count > 0)
            {
                var (current, currentTarget) = queue.Dequeue();
                current.position = currentTarget.position;
                current.rotation = currentTarget.rotation;
                for (var i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    var targetChild = currentTarget.Find(child.name);
                    if (targetChild == null)
                    {
                        Debug.LogWarning($"No target found for {child.name}");
                        continue;
                    }
                    queue.Enqueue((child, targetChild));
                }
            }
        }
    }
}