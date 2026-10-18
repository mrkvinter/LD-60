using Code.Game.Core;
using UnityEngine;

namespace Game.Scripts.Items
{
    /// <summary>
    /// Holds the items the editor scattered and let settle (<see cref="BakeMenu"/>). The game initializes them like
    /// any item placed by hand, then <see cref="ItemSpawner"/> takes them out and drops the container.
    /// </summary>
    public sealed class BakedItemLayout : ObjectRegistry.MonoRegistered<BakedItemLayout>
    {
        public const string BakeMenu = "Game/Items/Bake Item Layout";

        // The baked poses are already at rest, so the items start asleep. They also leave the container: a thousand
        // children of one parent share one transform hierarchy, and every move in it would be paid by all of them.
        public void Release()
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
