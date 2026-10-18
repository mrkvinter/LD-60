using System;
using RG.DefinitionSystem.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Scripts.Player
{
    [Serializable]
    public sealed class PlayerSettingsDef : SingletonDefinition
    {
        [SuffixLabel("m")] public float InteractDistance = 3f;
        [Min(1)] public int HandsCapacity = 5;
        [SuffixLabel("s")] public float RepeatInterval = 0.15f;
        [SuffixLabel("m/s")] public float ThrowSpeed = 3.5f;

        [Tooltip("Abilities the player has before any syringe.")]
        public PlayerAbility[] StartAbilities = Array.Empty<PlayerAbility>();
    }
}
