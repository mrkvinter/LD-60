using System;
using System.Collections.Generic;
using Game.Scripts.Items;

namespace Game.Scripts.Upgrades
{
    /// <summary>Points earned by filling racks with trash, and the upgrade levels bought with them.</summary>
    public sealed class UpgradeService : IDisposable
    {
        private readonly UpgradeSettingsDef settings;
        private readonly Dictionary<UpgradeKind, int> levels = new();
        private readonly Dictionary<Rack, Action> racks = new();
        private readonly HashSet<Rack> filled = new();

        public int Points { get; private set; }

        /// <summary>Racks filled up with trash at least once, the ones that gave points.</summary>
        public int FilledRacks => filled.Count;

        public event Action Changed;
        public event Action<int> PointsEarned;
        public event Action<Rack> RackFilled;
        public event Action<UpgradeKind, int, int> Bought;

        public UpgradeService(UpgradeSettingsDef settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public int Level(UpgradeKind kind) => levels.TryGetValue(kind, out var level) ? level : 0;

        public int MaxLevel(UpgradeKind kind) => settings.Levels(kind).Length;

        /// <summary>The bought level's values; null before the first level.</summary>
        public UpgradeLevel Current(UpgradeKind kind)
        {
            var level = Level(kind);
            return level > 0 ? settings.Levels(kind)[level - 1] : null;
        }

        /// <summary>The level on sale; null at the top.</summary>
        public UpgradeLevel Next(UpgradeKind kind)
        {
            var table = settings.Levels(kind);
            var level = Level(kind);
            return level < table.Length ? table[level] : null;
        }

        /// <summary>Sum of Amount over the bought levels, e.g. all the extra hand slots.</summary>
        public float TotalAmount(UpgradeKind kind)
        {
            var table = settings.Levels(kind);
            var total = 0f;
            for (var i = 0; i < Level(kind); i++) total += table[i].Amount;
            return total;
        }

        public bool CanBuy(UpgradeKind kind) => Next(kind) is { } next && Points >= next.Cost;

        public bool TryBuy(UpgradeKind kind)
        {
            if (!CanBuy(kind)) return false;

            var cost = Next(kind).Cost;
            Points -= cost;
            levels[kind] = Level(kind) + 1;
            Changed?.Invoke();
            Bought?.Invoke(kind, levels[kind], cost);
            return true;
        }

        public void AddPoints(int points)
        {
            if (points <= 0) return;

            Points += points;
            Changed?.Invoke();
            PointsEarned?.Invoke(points);
        }

        public void Register(Rack rack)
        {
            if (racks.ContainsKey(rack)) return;

            void OnChanged() => CheckFilled(rack);
            racks[rack] = OnChanged;
            rack.Changed += OnChanged;
            CheckFilled(rack);
        }

        public void Dispose()
        {
            foreach (var (rack, handler) in racks) rack.Changed -= handler;
            racks.Clear();
        }

        // Once per rack: taking items off and filling it again gives nothing.
        private void CheckFilled(Rack rack)
        {
            if (rack.Capacity == 0 || rack.Count < rack.Capacity || rack.StoredDef is not TrashDef) return;
            if (!filled.Add(rack)) return;

            // Changed even for zero points: the filled rack count moved.
            if (settings.PointsPerFilledRack > 0) AddPoints(settings.PointsPerFilledRack);
            else Changed?.Invoke();
            RackFilled?.Invoke(rack);
        }
    }
}
