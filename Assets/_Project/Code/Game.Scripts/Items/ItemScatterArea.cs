using System;
using Code.Game.Core;
using RG.DefinitionSystem.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Scripts.Items
{
    public sealed class ItemScatterArea : ObjectRegistry.MonoRegistered<ItemScatterArea>
    {
        [SerializeField] private Vector3 size = new(4f, 2f, 4f);

        [Tooltip("Items per square meter compared to an area of density 1. Adds up with the areas this one " +
                 "overlaps, so a small dense area piles junk on one spot of a big one.")]
        [SerializeField, Min(0f)] private float density = 1f;

        [Tooltip("Nothing is scattered inside, even where another area covers this one.")]
        [SerializeField] private bool excludeAll;

        [Tooltip("These items are never scattered inside, even where another area covers this one.")]
        [SerializeField] private DefRef<ItemDef>[] excludedItems = Array.Empty<DefRef<ItemDef>>();

        public Vector3 Size => size;
        public float Height => size.y;
        public bool ExcludesAll => excludeAll;

        public float Weight
        {
            get
            {
                var scale = transform.lossyScale;
                return Mathf.Abs(size.x * scale.x * size.z * scale.z) * density;
            }
        }

        public bool Excludes(ItemDef def)
        {
            if (excludeAll) return true;

            foreach (var excluded in excludedItems)
            {
                if (excluded.Id == def.Id) return true;
            }

            return false;
        }

        public bool Contains(Vector3 point)
        {
            var local = transform.InverseTransformPoint(point);
            return Mathf.Abs(local.x) <= size.x * 0.5f
                   && Mathf.Abs(local.y) <= size.y * 0.5f
                   && Mathf.Abs(local.z) <= size.z * 0.5f;
        }

        public Vector3 RandomTopPoint()
        {
            var local = new Vector3((Random.value - 0.5f) * size.x, size.y * 0.5f, (Random.value - 0.5f) * size.z);
            return transform.TransformPoint(local);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = excludeAll ? new Color(1f, 0.3f, 0.3f, 0.6f)
                : excludedItems.Length > 0 ? new Color(1f, 0.7f, 0.2f, 0.6f)
                : new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
