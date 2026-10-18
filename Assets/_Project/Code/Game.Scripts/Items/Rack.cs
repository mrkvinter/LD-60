using System;
using System.Collections.Generic;
using Code.Game.Scripts.EntitySystem;
using UnityEngine;

namespace Game.Scripts.Items
{
    public enum RackFilter
    {
        Any,
        Trash,
        Resource
    }

    public sealed class Rack : Entity, IItemContainer, IItemReceiver
    {
        private BaseItem[] slots = Array.Empty<BaseItem>();

        public Transform SlotRoot { get; }
        public RackType Type { get; }
        public RackFilter Filter { get; }

        public RackLayout Layout { get; }

        public ItemDef StoredDef { get; private set; }

        public int Count { get; private set; }
        public int Capacity => slots.Length;
        public ItemMode Mode => ItemMode.Stored;

        public IReadOnlyList<BaseItem> Slots => slots;

        public event Action Changed;

        public Rack(Transform slotRoot, RackType type = RackType.None, RackFilter filter = RackFilter.Any,
            RackLayout layout = null)
        {
            SlotRoot = slotRoot;
            Type = type;
            Filter = filter;
            Layout = layout;
        }

        public bool Accepts(BaseItem item) =>
            (Type == RackType.None || Type == item.Def.RackType) && Filter switch
            {
                RackFilter.Trash => item is TrashItem,
                RackFilter.Resource => item is ResourceItem,
                _ => true
            };

        public RackLayout LayoutFor(ItemDef def) => Layout ?? def.RackLayout;

        public bool CanPut(BaseItem item) => item != null && Accepts(item) && FindFreeSlot(item.Def, null) >= 0;

        public bool TryGetNextSlot(BaseItem item, out Vector3 localPosition) => TryGetNextSlot(item, null, out localPosition);

        /// <summary>Where <paramref name="item"/> would stand if put now from <paramref name="viewer"/>, in slot root space.</summary>
        public bool TryGetNextSlot(BaseItem item, Vector3? viewer, out Vector3 localPosition)
        {
            var index = CanPut(item) ? FindFreeSlot(item.Def, viewer) : -1;
            localPosition = index >= 0 ? LayoutFor(item.Def).GetPosition(index) : default;
            return index >= 0;
        }

        public void Put(BaseItem item, bool instant = false) => Put(item, null, instant);

        void IItemReceiver.Put(BaseItem item, Vector3? viewer) => Put(item, viewer);

        public void Put(BaseItem item, Vector3? viewer, bool instant = false)
        {
            if (!CanPut(item))
            {
                throw new InvalidOperationException($"{item?.Def} does not fit on the rack.");
            }

            var index = FindFreeSlot(item.Def, viewer);
            if (StoredDef == null)
            {
                StoredDef = item.Def;
                slots = new BaseItem[LayoutFor(item.Def).Capacity];
            }

            slots[index] = item;
            Count++;

            item.SetContainer(this);
            PlaceAt(index, instant);

            Changed?.Invoke();
        }

        public bool Take(BaseItem item)
        {
            var index = Array.IndexOf(slots, item);
            if (index < 0) return false;

            var layout = LayoutFor(StoredDef);
            var cell = layout.GetCell(index);
            for (var y = cell.y; y < layout.Count.y; y++)
            {
                var above = y + 1 < layout.Count.y ? slots[layout.GetIndex(new Vector3Int(cell.x, y + 1, cell.z))] : null;
                var current = layout.GetIndex(new Vector3Int(cell.x, y, cell.z));
                slots[current] = above;

                if (above == null) break;

                PlaceAt(current, false);
            }

            Count--;
            if (Count == 0)
            {
                StoredDef = null;
                slots = Array.Empty<BaseItem>();
            }

            Changed?.Invoke();
            return true;
        }

        public BaseItem TakeLast()
        {
            for (var i = slots.Length - 1; i >= 0; i--)
            {
                var item = slots[i];
                if (item == null) continue;

                Take(item);
                return item;
            }

            return null;
        }

        public BaseItem TakeNearest(Vector3? viewer)
        {
            if (Count == 0) return null;
            if (viewer == null) return TakeLast();

            var layout = LayoutFor(StoredDef);
            var local = SlotRoot != null ? SlotRoot.InverseTransformPoint(viewer.Value) : viewer.Value;
            var perLayer = layout.Count.x * layout.Count.z;
            var layerStart = Array.FindLastIndex(slots, slot => slot != null) / perLayer * perLayer;

            BaseItem nearest = null;
            var nearestDistance = float.MaxValue;
            for (var i = layerStart; i < layerStart + perLayer; i++)
            {
                if (slots[i] == null) continue;

                var offset = layout.GetPosition(i) - local;
                offset.y = 0f;
                if (offset.sqrMagnitude >= nearestDistance) continue;

                nearest = slots[i];
                nearestDistance = offset.sqrMagnitude;
            }

            Take(nearest);
            return nearest;
        }

        private int FindFreeSlot(ItemDef def, Vector3? viewer)
        {
            if (StoredDef != null && !Equals(StoredDef, def)) return -1;

            var layout = LayoutFor(def);
            var first = StoredDef == null ? (layout.Capacity > 0 ? 0 : -1) : Array.IndexOf(slots, null);
            if (first < 0 || viewer == null) return first;

            var local = SlotRoot != null ? SlotRoot.InverseTransformPoint(viewer.Value) : viewer.Value;
            var perLayer = layout.Count.x * layout.Count.z;
            var layerStart = first / perLayer * perLayer;

            var farthest = first;
            var farthestDistance = -1f;
            for (var i = layerStart; i < layerStart + perLayer; i++)
            {
                if (StoredDef != null && slots[i] != null) continue;

                var offset = layout.GetPosition(i) - local;
                offset.y = 0f;
                if (offset.sqrMagnitude <= farthestDistance) continue;

                farthest = i;
                farthestDistance = offset.sqrMagnitude;
            }

            return farthest;
        }

        private void PlaceAt(int index, bool instant)
        {
            var layout = LayoutFor(StoredDef);
            slots[index].Place(SlotRoot, layout.GetPosition(index), layout.SlotRotation, instant: instant);
        }
    }
}
