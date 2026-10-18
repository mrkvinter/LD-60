using System.Collections.Generic;
using Code.Game.Core;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Items
{
    public sealed class ItemSpawner
    {
        private readonly ObjectRegistry registry;

        public ItemSpawner(ObjectRegistry registry)
        {
            this.registry = registry;
        }
        
        public void Spawn()
        {
            var layouts = new List<BakedItemLayout>(registry.Get<BakedItemLayout>());
            if (layouts.Count > 0)
            {
                foreach (var layout in layouts)
                {
                    layout.Release();
                }

                return;
            }

            Debug.LogWarning($"No {nameof(BakedItemLayout)} in the scene; items are scattered at runtime. " +
                             $"Bake them with {BakedItemLayout.BakeMenu}.");

            var defaultPrefab = DefManager.GetSingletonDef<ItemSettingsDef>().DefaultPrefab;
            var items = new List<BaseItem>();

            foreach (var def in DefManager.GetDefMap<ItemDef>().DefinitionsEntries)
            {
                if (def.SpawnCount <= 0) continue;

                var prefab = def.Prefab != null ? def.Prefab : defaultPrefab;
                if (prefab == null)
                {
                    Debug.LogError($"Item '{def.Id}' has no prefab and {nameof(ItemSettingsDef)} has no " +
                                   $"{nameof(ItemSettingsDef.DefaultPrefab)}; it is not spawned.");
                    continue;
                }

                for (var i = 0; i < def.SpawnCount; i++)
                {
                    var adapter = Object.Instantiate(prefab).GetComponent<ItemAdapter>();
                    if (adapter == null)
                    {
                        Debug.LogError($"Prefab '{prefab.name}' of item '{def.Id}' has no {nameof(ItemAdapter)}.", prefab);
                        break;
                    }

                    adapter.Setup(def.Id);
                    adapter.Initialize();
                    items.Add(adapter.Entity);
                }
            }

            if (items.Count > 0) Scatter(items);
        }

        public void Scatter(IEnumerable<BaseItem> items)
        {
            var areas = new List<ItemScatterArea>(registry.Get<ItemScatterArea>());
            if (areas.Count == 0)
            {
                Debug.LogError($"No {nameof(ItemScatterArea)} in the scene; items stay where they are.");
                return;
            }

            var pending = new List<BaseItem>(items);
            foreach (var item in pending)
            {
                item.Release(ItemScatter.ParkingSpot, Quaternion.identity, Vector3.zero);
            }

            Physics.SyncTransforms();

            foreach (var item in pending)
            {
                var rotation = Random.rotation;
                if (!ItemScatter.TryPickSpot(areas, item.Def, item.View.LocalBounds, rotation, out var position))
                {
                    position = areas[0].transform.position;
                }

                item.Release(position, rotation, Vector3.zero);
                Physics.SyncTransforms();
            }
        }
    }
}
