using Code.Game.Core;
using UnityEngine;

namespace Game.Scripts.Items
{
    public sealed class ItemScatterArea : ObjectRegistry.MonoRegistered<ItemScatterArea>
    {
        [SerializeField] private Vector3 size = new(4f, 2f, 4f);

        public float Height => size.y;

        public Vector3 RandomTopPoint()
        {
            var local = new Vector3((Random.value - 0.5f) * size.x, size.y * 0.5f, (Random.value - 0.5f) * size.z);
            return transform.TransformPoint(local);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
