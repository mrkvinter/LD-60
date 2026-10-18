using System.Collections.Generic;
using System.Linq;
using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Items;
using UnityEngine;

namespace Game.Scripts.Upgrades
{
    public static class HandsAbilities
    {
        /// <summary>Puts every item in hand on a rack already holding its kind; the rest stay. Returns how many went.</summary>
        public static int Distribute(ItemHandsComponent hands, IEnumerable<Rack> racks, Vector3? viewer)
        {
            var racksList = racks.ToList();
            var moved = 0;

            foreach (var item in hands.Items.ToArray())
            {
                var rack = racksList.FirstOrDefault(candidate =>
                    candidate.IsActive && Equals(candidate.StoredDef, item.Def) && candidate.CanPut(item));
                if (rack == null || !hands.Remove(item)) continue;

                rack.Put(item, viewer);
                moved++;
            }

            return moved;
        }

        /// <summary>Takes up to <paramref name="count"/> loose items of <paramref name="def"/>, nearest first, as far as hands have room.</summary>
        public static int Pull(ItemHandsComponent hands, IEnumerable<BaseItem> items, ItemDef def, int count, Vector3 origin)
        {
            var room = Mathf.Min(count, hands.Capacity - hands.Count);
            if (def == null || room <= 0) return 0;

            var pulled = items
                .Where(item => item.Container == null && item.IsActive && Equals(item.Def, def))
                .OrderBy(item => item.GetComponent<ISpatialComponent>() is { } spatial
                    ? (spatial.Position - origin).sqrMagnitude
                    : 0f)
                .Take(room)
                .ToList();

            foreach (var item in pulled) hands.Put(item);

            return pulled.Count;
        }
    }
}
