using System;
using System.Linq;
using RG.DefinitionSystem.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Scripts.Items
{
    [Serializable]
    public sealed class ItemSettingsDef : SingletonDefinition
    {
        [InfoBox("@GetItemsInfo()")]
        public GameObject DefaultPrefab;
        public Color HighlightColor = new(1f, 0.85f, 0.4f, 1f);
        [SuffixLabel("s")] public float FlightDuration = 0.25f;

        [Tooltip("How long the item in hand takes to hide or show when switching items.")]
        [SuffixLabel("s")] public float SwapDuration = 0.15f;

        [Tooltip("Shows where the item in hand will stand on the aimed rack.")]
        public Material GhostMaterial;

        public string GetItemsInfo()
        {
            return $"Items: {DefManager.GetDefMap<ItemDef>().DefinitionsEntries.Select(e => e.SpawnCount).Sum()}";
        }
    }
}
