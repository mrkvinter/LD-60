using Code.Game.Core;
using UnityEngine;

namespace Game.Scripts.Items
{
    /// <summary>
    /// Holds the items the editor scattered and let settle (<see cref="BakeMenu"/>). The game initializes them like
    /// any item placed by hand, then the layout takes them out and drops the container.
    /// </summary>
    public sealed class BakedItemLayout : ObjectRegistry.MonoRegistered<BakedItemLayout>
    {
        public const string BakeMenu = "Game/Items/Bake Item Layout";

        // Saves point at items by these ids, so they follow the baked order and are given before any item initializes.
        private void Awake()
        {
            var adapters = GetComponentsInChildren<ItemAdapter>(true);
            for (var i = 0; i < adapters.Length; i++)
            {
                adapters[i].Id = i;
            }
        }

        // The baked poses are already at rest, so the items start asleep. They also leave the container: a thousand
        // children of one parent share one transform hierarchy, and every move in it would be paid by all of them.
        protected override void OnPostInitialize()
        {
            foreach (var adapter in GetComponentsInChildren<ItemAdapter>())
            {
                adapter.transform.SetParent(null, true);
                adapter.Entity?.Settle();
            }

            Destroy(gameObject);
        }
    }
}
