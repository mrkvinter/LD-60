using System;
using System.Collections.Generic;

namespace Game.Scripts.Construction
{
    /// <summary>The construction sites on the level by building; tells when one of them gets built.</summary>
    public sealed class ConstructionProgress : IDisposable
    {
        private readonly List<(BuildingDef Building, ConstructionSite Site)> sites = new();

        public event Action Changed;

        public void Register(BuildingDef building, ConstructionSite site)
        {
            if (building == null) throw new ArgumentNullException(nameof(building));
            if (site == null) throw new ArgumentNullException(nameof(site));
            if (sites.Exists(entry => entry.Site == site)) return;

            sites.Add((building, site));
            site.Built += RaiseChanged;
            if (site.IsBuilt) RaiseChanged();
        }

        /// <summary>Built sites of the building; sites that have not registered yet, e.g. still hidden, are not counted.</summary>
        public int BuiltCount(string buildingId)
        {
            var count = 0;
            foreach (var (building, site) in sites)
            {
                if (site.IsBuilt && building.Id == buildingId) count++;
            }

            return count;
        }

        public void Dispose()
        {
            foreach (var (_, site) in sites) site.Built -= RaiseChanged;
            sites.Clear();
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
