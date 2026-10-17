using System;
using System.Collections.Generic;
using R3;

namespace Game.Scripts.Items
{
    public sealed class SortingProgress : IDisposable
    {
        private readonly List<Rack> racks = new();
        private readonly List<BaseItem> items = new();
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

        public void Register(BaseItem item)
        {
            if (items.Contains(item)) return;

            items.Add(item);
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
