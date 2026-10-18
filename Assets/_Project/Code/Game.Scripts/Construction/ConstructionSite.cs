using System;
using System.Collections.Generic;
using System.Linq;
using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Items;
using UnityEngine;

namespace Game.Scripts.Construction
{
    public readonly struct BuildRequirement
    {
        public ResourceDef Resource { get; }
        public int Count { get; }

        public BuildRequirement(ResourceDef resource, int count)
        {
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            Count = count;
        }
    }

    /// <summary>An object waiting for resources: each one put in is spent, and once all are in it is built.</summary>
    public sealed class ConstructionSite : Entity, IItemReceiver
    {
        private readonly BuildRequirement[] requirements;
        private readonly int[] delivered;

        public string Name { get; }
        public IReadOnlyList<BuildRequirement> Requirements => requirements;
        public IReadOnlyList<int> Delivered => delivered;
        public bool IsBuilt { get; private set; }

        public event Action Changed;
        public event Action Built;

        public ConstructionSite(string name, IEnumerable<BuildRequirement> requirements)
        {
            Name = name;
            this.requirements = requirements.ToArray();
            delivered = new int[this.requirements.Length];
            IsBuilt = this.requirements.All(requirement => requirement.Count <= 0);
        }

        public bool CanPut(BaseItem item) => !IsBuilt && item != null && FindNeed(item.Def) >= 0;

        public void Put(BaseItem item, Vector3? viewer = null)
        {
            var index = item != null && !IsBuilt ? FindNeed(item.Def) : -1;
            if (index < 0)
            {
                throw new InvalidOperationException($"{Name} does not need {item?.Def}.");
            }

            delivered[index]++;
            item.Consume();
            Changed?.Invoke();

            for (var i = 0; i < requirements.Length; i++)
            {
                if (delivered[i] < requirements[i].Count) return;
            }

            IsBuilt = true;
            Built?.Invoke();
        }

        /// <summary>Builds at once as if every resource had been put in. For cheats.</summary>
        public void Complete()
        {
            if (IsBuilt) return;

            for (var i = 0; i < requirements.Length; i++)
            {
                delivered[i] = Math.Max(delivered[i], requirements[i].Count);
            }

            IsBuilt = true;
            Changed?.Invoke();
            Built?.Invoke();
        }

        private int FindNeed(ItemDef def)
        {
            for (var i = 0; i < requirements.Length; i++)
            {
                if (Equals(requirements[i].Resource, def) && delivered[i] < requirements[i].Count) return i;
            }

            return -1;
        }
    }
}
