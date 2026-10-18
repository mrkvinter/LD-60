using System;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Upgrades
{
    public enum UpgradeKind
    {
        HandsCapacity,
        Highlight,
        Distribute,
        Pull
    }

    /// <summary>One level of an upgrade. What Amount means depends on the upgrade.</summary>
    [Serializable]
    public sealed class UpgradeLevel
    {
        [Min(0)] public int Cost = 1;
        public float Amount;
        [Min(0f)] public float Cooldown;
    }

    [Serializable]
    public sealed class UpgradeSettingsDef : SingletonDefinition
    {
        [Tooltip("Points for each rack filled up with trash for the first time.")]
        [Min(0)] public int PointsPerFilledRack = 1;

        [Tooltip("Amount: slots this level adds on top of PlayerSettingsDef.HandsCapacity.")]
        public UpgradeLevel[] HandsCapacity = Array.Empty<UpgradeLevel>();

        [Tooltip("Amount: seconds the same items stay lit. Cooldown: seconds from use to use.")]
        public UpgradeLevel[] Highlight = Array.Empty<UpgradeLevel>();

        [Tooltip("Puts everything in hand on racks that already hold that kind. Only Cooldown is used.")]
        public UpgradeLevel[] Distribute = Array.Empty<UpgradeLevel>();

        [Tooltip("Amount: how many of the same items fly into hand. Cooldown: seconds from use to use.")]
        public UpgradeLevel[] Pull = Array.Empty<UpgradeLevel>();

        public UpgradeLevel[] Levels(UpgradeKind kind) => kind switch
        {
            UpgradeKind.HandsCapacity => HandsCapacity,
            UpgradeKind.Highlight => Highlight,
            UpgradeKind.Distribute => Distribute,
            UpgradeKind.Pull => Pull,
            _ => Array.Empty<UpgradeLevel>()
        };
    }
}
