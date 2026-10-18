using System.Collections.Generic;
using Game.Scripts.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Code.Game.Editor.Lighting
{
    /// <summary>
    /// Fills one <see cref="LightProbeGroup"/> over every <see cref="ItemScatterArea"/> and around every rack. Items
    /// are dynamic, so baked lighting reaches them only through probes; the probes stand on whatever an item dropped
    /// in the area would land on.
    /// </summary>
    public static class ItemLightProbes
    {
        private const string Menu = "Game/Lighting/Generate Item Light Probes";
        private const string UndoName = "Generate Item Light Probes";
        private const string GroupName = "Item Light Probes";

        // Ground step between probe columns, in meters.
        private const float Spacing = 2.5f;

        // Above the ground. The lowest sits right on the floor, so items lying there are inside the probe hull: outside
        // it Unity extrapolates, and near the hull's edges that blows up into light thousands of times too bright. The
        // upper one is where the player carries them.
        private static readonly float[] Heights = { 0.05f, 1.8f };

        // A column that starts in a wall is moved toward the area's middle by these steps, so the hull still reaches
        // close to the walls instead of ending a whole step short of them.
        private static readonly float[] WallNudges = { 0.5f, 1f };

        private const float RackTopOffset = 0.3f;

        // Items fall from an area's top, so the ground may be well below its box.
        private const float MaxFall = 30f;
        private const float MinGroundNormalY = 0.5f;
        private const float GroundOffset = 0.05f;

        // A probe this close to the level picks up the dark inside of a wall or a floor.
        private const float Clearance = 0.15f;

        // Probes from overlapping areas closer than this are merged.
        private const float MergeDistance = Spacing * 0.5f;

        private const float RackMargin = 0.5f;
        private const float InsideCheckDistance = 100f;

        [MenuItem(Menu)]
        private static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[LightProbes] Generate outside Play mode.");
                return;
            }

            var areas = Object.FindObjectsByType<ItemScatterArea>(FindObjectsInactive.Exclude);
            if (areas.Length == 0)
            {
                Debug.LogError($"[LightProbes] No {nameof(ItemScatterArea)} in the open scenes.");
                return;
            }

            var racks = Object.FindObjectsByType<RackAdapter>(FindObjectsInactive.Exclude);

            Physics.SyncTransforms();
            var probes = new ProbeSet();
            foreach (var area in areas)
            {
                AddArea(probes, area);
            }

            foreach (var rack in racks)
            {
                AddRack(probes, rack);
            }

            // Fewer than four can't be tetrahedralized.
            if (probes.Positions.Count < 4)
            {
                Debug.LogError($"[LightProbes] Only {probes.Positions.Count} probes found ground. Do the scatter " +
                               "areas stand over the level's colliders?");
                return;
            }

            Undo.SetCurrentGroupName(UndoName);
            var undoGroup = Undo.GetCurrentGroup();

            var group = GetOrCreateGroup();
            Undo.RecordObject(group, UndoName);
            group.probePositions = probes.Positions.ConvertAll(group.transform.InverseTransformPoint).ToArray();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
            Selection.activeGameObject = group.gameObject;

            Debug.Log($"[LightProbes] {probes.Positions.Count} probes over {areas.Length} scatter areas and " +
                      $"{racks.Length} racks. Columns without ground: {probes.NoGround}, probes in or against the " +
                      $"level: {probes.Blocked}. Generate Lighting to apply them.", group);
        }

        private static void AddArea(ProbeSet probes, ItemScatterArea area)
        {
            // Nothing lies in an exclusion, and its box stands over things like the portal's top, right under a lamp.
            if (area.ExcludesAll) return;

            var size = area.Size;
            var scale = area.transform.lossyScale;
            var countX = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(size.x * scale.x) / Spacing));
            var countZ = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(size.z * scale.z) / Spacing));
            var fall = Mathf.Abs(size.y * scale.y) + MaxFall;
            var middle = area.transform.position;

            // Edges included, so an area narrower than a step still gets its columns.
            for (var x = 0; x <= countX; x++)
            {
                for (var z = 0; z <= countZ; z++)
                {
                    var local = new Vector3((x / (float)countX - 0.5f) * size.x, size.y * 0.5f,
                        (z / (float)countZ - 0.5f) * size.z);
                    var start = area.transform.TransformPoint(local);
                    if (AddColumn(probes, start, fall, null)) continue;

                    var inward = new Vector3(middle.x - start.x, 0f, middle.z - start.z).normalized;
                    var placed = false;
                    foreach (var nudge in WallNudges)
                    {
                        placed = AddColumn(probes, start + inward * nudge, fall, null);
                        if (placed) break;
                    }

                    if (!placed) probes.NoGround++;
                }
            }
        }

        // Columns at the corners just outside the rack, with one more probe over its top for tall shelves.
        private static void AddRack(ProbeSet probes, RackAdapter rack)
        {
            var renderers = rack.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            bounds.Expand(new Vector3(RackMargin * 2f, 0f, RackMargin * 2f));
            var start = bounds.max.y + RackMargin;
            var fall = bounds.size.y + MaxFall;

            foreach (var x in new[] { bounds.min.x, bounds.max.x })
            {
                foreach (var z in new[] { bounds.min.z, bounds.max.z })
                {
                    if (!AddColumn(probes, new Vector3(x, start, z), fall, bounds.max.y + RackTopOffset))
                    {
                        probes.NoGround++;
                    }
                }
            }
        }

        // True when at least one probe of the column found a place.
        private static bool AddColumn(ProbeSet probes, Vector3 start, float fall, float? top)
        {
            if (IsInsideLevel(start) || !TryFindGround(start, fall, out var ground)) return false;

            var placed = false;
            foreach (var height in Heights)
            {
                if (!TryAdd(probes, ground, ground + Vector3.up * height, ref placed)) return placed;
            }

            if (top is { } y && y > ground.y + Heights[Heights.Length - 1])
            {
                TryAdd(probes, ground, new Vector3(ground.x, y, ground.z), ref placed);
            }

            return placed;
        }

        // False once the way up from the ground is blocked: everything higher is under the same ceiling.
        private static bool TryAdd(ProbeSet probes, Vector3 ground, Vector3 probe, ref bool placed)
        {
            var from = ground + Vector3.up * GroundOffset;
            if (CastLevel(from, Vector3.up, probe.y - from.y, false, out _))
            {
                probes.Blocked++;
                return false;
            }

            // The floor probe is meant to touch the floor; the clearance check would always reject it.
            var onFloor = probe.y - ground.y < Clearance;
            if ((!onFloor && IsTooClose(probe)) || IsInsideLevel(probe))
            {
                probes.Blocked++;
                return true;
            }

            probes.Add(probe);
            placed = true;
            return true;
        }

        private static bool TryFindGround(Vector3 start, float distance, out Vector3 ground)
        {
            ground = default;
            if (!CastLevel(start, Vector3.down, distance, false, out var hit) || hit.normal.y < MinGroundNormalY)
            {
                return false;
            }

            ground = hit.point;
            return true;
        }

        // The nearest surface above is seen from behind: the point is inside a wall or under a floor.
        private static bool IsInsideLevel(Vector3 point)
        {
            if (!CastLevel(point, Vector3.up, InsideCheckDistance, true, out var nearest)) return false;

            return !CastLevel(point, Vector3.up, InsideCheckDistance, false, out var front) ||
                   nearest.distance < front.distance - 0.001f;
        }

        private static bool IsTooClose(Vector3 point)
        {
            foreach (var collider in Physics.OverlapSphere(point, Clearance, Physics.DefaultRaycastLayers,
                         QueryTriggerInteraction.Ignore))
            {
                if (IsLevel(collider)) return true;
            }

            return false;
        }

        private static bool CastLevel(Vector3 origin, Vector3 direction, float distance, bool backfaces,
            out RaycastHit nearest)
        {
            RaycastHit[] hits;
            var hitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = backfaces;
            try
            {
                hits = Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
            }
            finally
            {
                Physics.queriesHitBackfaces = hitBackfaces;
            }

            nearest = default;
            var found = false;
            foreach (var hit in hits)
            {
                if (!IsLevel(hit.collider) || (found && hit.distance >= nearest.distance)) continue;

                nearest = hit;
                found = true;
            }

            return found;
        }

        // Items, the player and anything else with a body pass through; probes care about the static level.
        private static bool IsLevel(Collider collider) => !collider.isTrigger && collider.attachedRigidbody == null;

        private static LightProbeGroup GetOrCreateGroup()
        {
            foreach (var existing in Object.FindObjectsByType<LightProbeGroup>(FindObjectsInactive.Exclude))
            {
                if (existing.name == GroupName) return existing;
            }

            var container = new GameObject(GroupName);
            Undo.RegisterCreatedObjectUndo(container, UndoName);
            return container.AddComponent<LightProbeGroup>();
        }

        private sealed class ProbeSet
        {
            private readonly HashSet<Vector3Int> cells = new();

            public readonly List<Vector3> Positions = new();
            public int NoGround;
            public int Blocked;

            public void Add(Vector3 position)
            {
                if (cells.Add(Vector3Int.FloorToInt(position / MergeDistance))) Positions.Add(position);
            }
        }
    }
}
