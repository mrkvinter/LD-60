using System;
using System.Collections.Generic;

namespace Game.Scripts.Construction
{
    /// <summary>The construction sites on the level by building; tells when one of them gets built.</summary>
    public sealed class ConstructionProgress : IDisposable
    {
        private readonly List<(BuildingDef Building, ConstructionSite Site, Action OnBuilt)> sites = new();

        public event Action Changed;
        public event Action<BuildingDef> Built;

        public void Register(BuildingDef building, ConstructionSite site)
        {
            if (building == null) throw new ArgumentNullException(nameof(building));
            if (site == null) throw new ArgumentNullException(nameof(site));
            if (sites.Exists(entry => entry.Site == site)) return;

            void OnBuilt()
            {
                Built?.Invoke(building);
                Changed?.Invoke();
            }

            sites.Add((building, site, OnBuilt));
            site.Built += OnBuilt;
            if (site.IsBuilt) Changed?.Invoke();
        }

        /// <summary>Built sites of the building; sites that have not registered yet, e.g. still hidden, are not counted.</summary>
        public int BuiltCount(string buildingId)
        {
            var count = 0;
            foreach (var (building, site, _) in sites)
            {
                if (site.IsBuilt && building.Id == buildingId) count++;
            }

            return count;
        }

        public int BuiltTotal()
        {
            var count = 0;
            foreach (var (_, site, _) in sites)
            {
                if (site.IsBuilt) count++;
            }

            return count;
        }

        public void Dispose()
        {
            foreach (var (_, site, onBuilt) in sites) site.Built -= onBuilt;
            sites.Clear();
        }
    }
}
