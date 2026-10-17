using System;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Items
{
    [Serializable]
    public abstract class ItemDef : Definition
    {
        public string Name;

        [Tooltip("What the spawner instantiates, pivot at the bottom. Empty means ItemSettingsDef.DefaultPrefab.")]
        public GameObject Prefab;

        [Tooltip("How many copies the spawner scatters over the level. Items placed in the scene by hand come on top.")]
        [Min(0)] public int SpawnCount;

        [Tooltip("Scale of the item while carried, so big things do not cover the view.")]
        [Min(0.01f)] public float HeldScale = 1f;

        [Tooltip("Which racks take these items. Racks of another type keep them off.")]
        public RackType RackType;

        [Tooltip("How these items stand on a rack.")]
        public RackLayout RackLayout = new();

        public abstract BaseItem CreateItem(IItemView view);
    }
}
