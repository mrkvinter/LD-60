using System;
using Game.Scripts.Items;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Construction
{
    /// <summary>What a construction site needs before its object appears.</summary>
    [Serializable]
    public sealed class BuildingDef : Definition
    {
        public string Name;
        public ResourceCost[] Cost = Array.Empty<ResourceCost>();
    }

    [Serializable]
    public sealed class ResourceCost
    {
        public DefRef<ResourceDef> Resource;
        [Min(1)] public int Count = 1;
    }
}
