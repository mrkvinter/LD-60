using System;
using RG.DefinitionSystem.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Scripts.Items
{
    [Serializable]
    public sealed class ItemSettingsDef : SingletonDefinition
    {
        public GameObject DefaultPrefab;
        public Color HighlightColor = new(1f, 0.85f, 0.4f, 1f);
        [SuffixLabel("s")] public float FlightDuration = 0.25f;

        [Tooltip("Shows where the item in hand will stand on the aimed rack.")]
        public Material GhostMaterial;
    }
}
