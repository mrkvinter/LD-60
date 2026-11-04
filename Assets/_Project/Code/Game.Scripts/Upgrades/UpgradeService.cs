using System;
using System.Collections.Generic;
using Game.Scripts.Construction;
using Game.Scripts.Items;
using Game.Scripts.SaveLoadSystem;

namespace Game.Scripts.Upgrades
{
    /// <summary>Points earned by filling racks with trash and by building, and the upgrade levels bought with them.</summary>
    public sealed class UpgradeService : IDisposable, IPersistent
    {
        private readonly UpgradeSettingsDef settings;
        private readonly Dictionary<UpgradeKind, int> levels = new();
        private readonly Dictionary<Rack, Action> racks = new();
        private readonly HashSet<Rack> filled = new();
        private readonly HashSet<string> loadedFilled = new();
        private readonly Dictionary<ConstructionSite, Action> sites = new();

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

        public void Save(GameSaveData data)
        {
            var saved = new SaveData { Points = Points, Levels = new Dictionary<UpgradeKind, int>(levels) };
            foreach (var rack in filled)
            {
                if (rack.Id != null) saved.FilledRacks.Add(rack.Id);
            }

            data.Upgrades = saved;
        }

        // Points are set, not added: whatever racks and sites paid while loading is overwritten.
        public void Load(GameSaveData data)
        {
            var saved = data.Upgrades;
            if (saved == null) return;

            Points = Math.Max(0, saved.Points);

            levels.Clear();
            foreach (var (kind, level) in saved.Levels ?? new Dictionary<UpgradeKind, int>())
            {
                var clamped = Math.Clamp(level, 0, MaxLevel(kind));
                if (clamped > 0) levels[kind] = clamped;
            }

            loadedFilled.Clear();
            loadedFilled.UnionWith(saved.FilledRacks ?? new List<string>());
            filled.Clear();
            foreach (var rack in racks.Keys)
            {
                if (IsLoadedFilled(rack)) filled.Add(rack);
            }

            Changed?.Invoke();
        }

        public void Register(Rack rack)
        {
            if (racks.ContainsKey(rack)) return;

            void OnChanged() => CheckFilled(rack);
            racks[rack] = OnChanged;
            rack.Changed += OnChanged;

            // A rack that paid before the save, e.g. one inside a building that loads later.
            if (IsLoadedFilled(rack)) filled.Add(rack);
            CheckFilled(rack);
        }

        [Serializable]
        public class SaveData
        {
            public int Points;
            public Dictionary<UpgradeKind, int> Levels = new();
            public List<string> FilledRacks = new();
        }

        private bool IsLoadedFilled(Rack rack) => rack.Id != null && loadedFilled.Contains(rack.Id);

        public void Register(BuildingDef building, ConstructionSite site)
        {
            if (building == null || site == null || building.Points <= 0 || sites.ContainsKey(site)) return;

            void OnBuilt() => AddPoints(building.Points);
            sites[site] = OnBuilt;
            site.Built += OnBuilt;
        }

        public void Dispose()
        {
            foreach (var (rack, handler) in racks) rack.Changed -= handler;
            racks.Clear();
            foreach (var (site, handler) in sites) site.Built -= handler;
            sites.Clear();
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
