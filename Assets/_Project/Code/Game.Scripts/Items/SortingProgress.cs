using System;
using System.Collections.Generic;
using R3;

namespace Game.Scripts.Items
{
    public sealed class SortingProgress : IDisposable
    {
        private readonly List<Rack> racks = new();
        private readonly List<BaseItem> items = new();
        private readonly Dictionary<int, BaseItem> byId = new();
        private readonly ReactiveProperty<int> stored = new();
        private readonly ReactiveProperty<int> total = new();

        public IReadOnlyList<Rack> Racks => racks;
        public IReadOnlyList<BaseItem> Items => items;
        public ReadOnlyReactiveProperty<int> Stored => stored;
        public ReadOnlyReactiveProperty<int> Total => total;

        public bool IsComplete => total.Value > 0 && stored.Value == total.Value;

        public void Register(Rack rack)
        {
            if (racks.Contains(rack)) return;

            racks.Add(rack);
            rack.Changed += Recount;
            Recount();
        }

        public BaseItem Find(int id) => byId.TryGetValue(id, out var item) ? item : null;

        public void Register(BaseItem item)
        {
            if (items.Contains(item)) return;

            items.Add(item);
            if (item.Id >= 0) byId[item.Id] = item;
            total.Value = items.Count;
        }

        public void Unregister(BaseItem item)
        {
            if (!items.Remove(item)) return;

            if (item.Id >= 0) byId.Remove(item.Id);
            total.Value = items.Count;
        }

        public void Dispose()
        {
            foreach (var rack in racks)
            {
                rack.Changed -= Recount;
            }

            racks.Clear();
            items.Clear();
            byId.Clear();
            stored.Dispose();
            total.Dispose();
        }

        private void Recount()
        {
            var onRacks = 0;
            foreach (var rack in racks)
            {
                onRacks += rack.Count;
            }

            stored.Value = onRacks;
        }
    }
}
