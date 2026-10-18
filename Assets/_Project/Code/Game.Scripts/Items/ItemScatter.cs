using System.Collections.Generic;
using Code.Game.Scripts.EntitySystem;
using UnityEngine;

namespace Game.Scripts.Items
{
    /// <summary>Finds free spots for items over <see cref="ItemScatterArea"/>s, at runtime and in the editor bake.</summary>
    public static class ItemScatter
    {
        private const int Attempts = 30;
        private const float SurfaceOffset = 0.01f;

        /// <summary>Out of the level, where an item waiting for its spot is not in the way of the casts.</summary>
        public static readonly Vector3 ParkingSpot = Vector3.down * 1000f;

        /// <summary>
        /// Lays an item with this collision box and rotation on top of whatever is under a random point of an
        /// area that takes it, touching nothing. The rotation is random in every axis, so it may rest on a corner
        /// and needs physics to tip it over.
        /// </summary>
        public static bool TryPickSpot(IReadOnlyList<ItemScatterArea> areas, ItemDef def, Bounds bounds,
            Quaternion rotation, out Vector3 position)
        {
            var offset = rotation * bounds.center;

            for (var i = 0; i < Attempts; i++)
            {
                var area = PickArea(areas, def);
                if (area == null) break;

                var start = area.RandomTopPoint() + offset;

                // A cast does not see what it starts inside, so a start buried in a pile would land in it.
                if (Physics.CheckBox(start, bounds.extents, rotation, Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (!Physics.BoxCast(start, bounds.extents, Vector3.down, out var hit, rotation, area.Height + 1f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var owner = hit.collider.GetComponentInParent<IEntityAdapter>();
                if (owner != null && owner is not ItemAdapter) continue;

                var landing = start + Vector3.down * hit.distance - offset + Vector3.up * SurfaceOffset;
                if (IsExcluded(areas, def, landing)) continue;

                position = landing;
                return true;
            }

            position = default;
            return false;
        }

        // Weighted by footprint and density, so items spread evenly instead of crowding the small areas,
        // unless an area asks for a crowd.
        private static ItemScatterArea PickArea(IReadOnlyList<ItemScatterArea> areas, ItemDef def)
        {
            var total = 0f;
            foreach (var area in areas)
            {
                if (Takes(area, def)) total += area.Weight;
            }

            var roll = Random.value * total;
            ItemScatterArea last = null;
            foreach (var area in areas)
            {
                if (!Takes(area, def)) continue;

                last = area;
                roll -= area.Weight;
                if (roll <= 0f) return area;
            }

            return last;
        }

        private static bool Takes(ItemScatterArea area, ItemDef def) => area.Weight > 0f && !area.Excludes(def);

        private static bool IsExcluded(IReadOnlyList<ItemScatterArea> areas, ItemDef def, Vector3 point)
        {
            foreach (var area in areas)
            {
                if (area.Excludes(def) && area.Contains(point)) return true;
            }

            return false;
        }
    }
}
