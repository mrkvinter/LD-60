using System.Collections.Generic;
using Code.Game.Core;
using Code.Game.Scripts.EntitySystem;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Items
{
    public sealed class ItemSpawner
    {
        private const int Attempts = 10;
        private const float SurfaceOffset = 0.01f;

        private readonly ObjectRegistry registry;

        public ItemSpawner(ObjectRegistry registry)
        {
            this.registry = registry;
        }

        public void Spawn()
        {
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

            foreach (var item in items)
            {
                var (position, rotation) = PickSpot(areas);
                item.Release(position, rotation, Vector3.zero);

                Physics.SyncTransforms();
            }
        }

        private static (Vector3 position, Quaternion rotation) PickSpot(List<ItemScatterArea> areas)
        {
            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            for (var i = 0; i < Attempts; i++)
            {
                var area = areas[Random.Range(0, areas.Count)];
                if (!Physics.Raycast(area.RandomTopPoint(), Vector3.down, out var hit, area.Height + 1f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var owner = hit.collider.GetComponentInParent<IEntityAdapter>();
                if (owner != null && owner is not ItemAdapter) continue;

                return (hit.point + Vector3.up * SurfaceOffset, rotation);
            }

            return (areas[0].transform.position, rotation);
        }
    }
}
