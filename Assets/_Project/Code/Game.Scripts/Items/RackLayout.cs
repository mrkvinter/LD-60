using System;
using UnityEngine;

namespace Game.Scripts.Items
{
    [Serializable]
    public sealed class RackLayout
    {
        [Tooltip("Slots along X, layers along Y, rows along Z.")]
        public Vector3Int Count = Vector3Int.one;

        [Tooltip("Distance between the pivots of neighbouring slots along each axis, in meters.")]
        public Vector3 Spacing = Vector3.one;

        [Tooltip("Shift of the whole grid from the middle of the rack, in meters.")]
        public Vector3 Offset;

        public int Capacity => Mathf.Max(0, Count.x) * Mathf.Max(0, Count.y) * Mathf.Max(0, Count.z);

        public Vector3Int GetCell(int index)
        {
            var layer = Count.x * Count.z;
            return new Vector3Int(index % Count.x, index / layer, index % layer / Count.x);
        }

        public int GetIndex(Vector3Int cell) => (cell.y * Count.z + cell.z) * Count.x + cell.x;

        public Vector3 GetPosition(int index)
        {
            var cell = GetCell(index);
            return Offset + new Vector3(
                (cell.x - (Count.x - 1) * 0.5f) * Spacing.x,
                cell.y * Spacing.y,
                (cell.z - (Count.z - 1) * 0.5f) * Spacing.z);
        }
    }
}
