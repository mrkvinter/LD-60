using System;
using System.Collections.Generic;

namespace Game.Scripts.Items
{
    public sealed class ResourceStorage : IDisposable
    {
        private readonly List<Rack> racks = new();
        private readonly Dictionary<ResourceDef, int> counts = new();

        public IReadOnlyDictionary<ResourceDef, int> Counts => counts;
        public int Total { get; private set; }

        public event Action Changed;

        public int Count(ResourceDef def) => counts.TryGetValue(def, out var count) ? count : 0;

        public void Register(Rack rack)
        {
            if (racks.Contains(rack)) return;

            racks.Add(rack);
            rack.Changed += Recount;
            Recount();
        }

        public void Dispose()
        {
            foreach (var rack in racks)
            {
                rack.Changed -= Recount;
            }

            racks.Clear();
            counts.Clear();
        }

        private void Recount()
        {
            counts.Clear();
            Total = 0;

            foreach (var rack in racks)
            {
                foreach (var item in rack.Slots)
                {
                    if (item is not ResourceItem resource) continue;

                    counts[resource.Def] = Count(resource.Def) + 1;
                    Total++;
                }
            }

            Changed?.Invoke();
        }
    }
}
