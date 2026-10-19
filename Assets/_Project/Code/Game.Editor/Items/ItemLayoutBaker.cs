using System.Collections.Generic;
using Game.Scripts.Items;
using RG.DefinitionSystem.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Game.Editor.Items
{
    public static class ItemLayoutBaker
    {
        private const string UndoName = "Bake Item Layout";
        private const float StepTime = 0.02f;
        private const int MaxSteps = 1500;
        private const int SleepCheckInterval = 25;
        private const float LostDepth = 5f;
        private const int MaxRounds = 20;

        [MenuItem(BakedItemLayout.BakeMenu)]
        private static void BakeFromMenu()
        {
            var layouts = FindInScenes<BakedItemLayout>();
            Bake(layouts.Count > 0 ? layouts[0] : CreateLayout());
        }

        public static void Bake(BakedItemLayout layout)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[ItemLayout] Bake outside Play mode.");
                return;
            }

            if (!DefManager.IsInitialized)
            {
                Debug.LogError("[ItemLayout] Definitions are not loaded; nothing to bake.");
                return;
            }

            var areas = FindInScenes<ItemScatterArea>();
            if (areas.Count == 0)
            {
                Debug.LogError($"[ItemLayout] No {nameof(ItemScatterArea)} in the open scenes.");
                return;
            }

            Undo.SetCurrentGroupName(UndoName);
            var undoGroup = Undo.GetCurrentGroup();

            Clear(layout);

            var bodies = new List<Rigidbody>();
            var kinds = new Dictionary<Rigidbody, ItemDef>();
            var placed = CountPlacedByHand();
            int steps = 0, missed = 0, lost = 0, rounds = 0;
            while (rounds < MaxRounds)
            {
                rounds++;
                Spawn(layout, areas, bodies, kinds, placed, ref missed);
                steps += Settle(bodies);
                lost += DropLost(bodies, kinds, placed, areas);

                if (Shortage(placed).Count == 0) break;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);

            var asleep = bodies.FindAll(body => body.IsSleeping()).Count;
            Debug.Log($"[ItemLayout] Baked {bodies.Count} items in {rounds} rounds, {steps * StepTime:0.#} s of " +
                      $"physics, {asleep} at rest. Dropped again: no free spot {missed}, fell out of the level {lost}.",
                      layout);

            var shortage = Shortage(placed);
            if (shortage.Count > 0)
            {
                Debug.LogError($"[ItemLayout] Still short after {MaxRounds} rounds, their racks can never be filled: " +
                               string.Join(", ", shortage), layout);
            }
        }

        public static void Clear(BakedItemLayout layout)
        {
            for (var i = layout.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(layout.transform.GetChild(i).gameObject);
            }
        }

        private static BakedItemLayout CreateLayout()
        {
            var container = new GameObject("Baked Items");
            SceneManager.MoveGameObjectToScene(container, SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(container, UndoName);
            return container.AddComponent<BakedItemLayout>();
        }

        // Tops every kind up to its SpawnCount.
        private static void Spawn(BakedItemLayout layout, List<ItemScatterArea> areas, List<Rigidbody> bodies,
            Dictionary<Rigidbody, ItemDef> kinds, Dictionary<ItemDef, int> placed, ref int missed)
        {
            var defaultPrefab = DefManager.GetSingletonDef<ItemSettingsDef>().DefaultPrefab;

            foreach (var def in DefManager.GetDefMap<ItemDef>().DefinitionsEntries)
            {
                if (def.SpawnCount <= 0) continue;

                var prefab = def.Prefab != null ? def.Prefab : defaultPrefab;
                if (prefab == null)
                {
                    Debug.LogError($"[ItemLayout] Item '{def.Id}' has no prefab and no default prefab; skipped.");
                    continue;
                }

                var have = placed.TryGetValue(def, out var count) ? count : 0;
                for (var i = have; i < def.SpawnCount; i++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, layout.transform);
                    if (!instance.TryGetComponent<ItemAdapter>(out var adapter) ||
                        !instance.TryGetComponent<Rigidbody>(out var body))
                    {
                        Debug.LogError($"[ItemLayout] Prefab '{prefab.name}' of item '{def.Id}' needs an " +
                                       $"{nameof(ItemAdapter)} and a {nameof(Rigidbody)} on its root.", prefab);
                        Object.DestroyImmediate(instance);
                        break;
                    }

                    adapter.Setup(def.Id);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(adapter);

                    // Out of the way of its own casts.
                    instance.transform.position = ItemScatter.ParkingSpot;
                    Physics.SyncTransforms();

                    var rotation = Random.rotation;
                    if (!ItemScatter.TryPickSpot(areas, def, adapter.LocalBounds, rotation, out var position))
                    {
                        Object.DestroyImmediate(instance);
                        missed++;
                        continue;
                    }

                    instance.transform.SetPositionAndRotation(position, rotation);
                    Physics.SyncTransforms();

                    Undo.RegisterCreatedObjectUndo(instance, UndoName);
                    bodies.Add(body);
                    kinds[body] = def;
                    placed[def] = placed.TryGetValue(def, out var placedCount) ? placedCount + 1 : 1;
                }
            }
        }

        private static List<string> Shortage(Dictionary<ItemDef, int> placed)
        {
            var shortage = new List<string>();
            foreach (var def in DefManager.GetDefMap<ItemDef>().DefinitionsEntries)
            {
                if (def.SpawnCount <= 0) continue;

                var have = placed.TryGetValue(def, out var count) ? count : 0;
                if (have < def.SpawnCount) shortage.Add($"{def.Id} {have}/{def.SpawnCount}");
            }

            return shortage;
        }

        // Steps physics until every item sleeps. Everything else in the scene is held still meanwhile, so the bake
        // moves nothing but the items it made.
        private static int Settle(List<Rigidbody> bodies)
        {
            var baked = new HashSet<Rigidbody>(bodies);
            var held = new List<Rigidbody>();
            foreach (var body in FindInScenes<Rigidbody>())
            {
                if (baked.Contains(body) || body.isKinematic) continue;

                body.isKinematic = true;
                held.Add(body);
            }

            var simulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            var steps = 0;
            try
            {
                while (steps < MaxSteps)
                {
                    Physics.Simulate(StepTime);
                    steps++;

                    if (steps % SleepCheckInterval != 0) continue;
                    if (bodies.TrueForAll(body => body.IsSleeping())) break;
                    if (EditorUtility.DisplayCancelableProgressBar(UndoName, "Letting the items settle",
                            (float)steps / MaxSteps))
                    {
                        break;
                    }
                }
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                foreach (var body in held)
                {
                    body.isKinematic = false;
                }

                EditorUtility.ClearProgressBar();
            }

            return steps;
        }

        // Through a gap in the floor or off an edge: such items would never be found in the game.
        private static int DropLost(List<Rigidbody> bodies, Dictionary<Rigidbody, ItemDef> kinds,
            Dictionary<ItemDef, int> placed, List<ItemScatterArea> areas)
        {
            var floor = float.MaxValue;
            foreach (var area in areas)
            {
                floor = Mathf.Min(floor, area.transform.position.y - area.Height * 0.5f);
            }

            return bodies.RemoveAll(body =>
            {
                if (body.position.y > floor - LostDepth) return false;

                var def = kinds[body];
                kinds.Remove(body);
                placed[def]--;
                Undo.DestroyObjectImmediate(body.gameObject);
                return true;
            });
        }

        // Items put in the scene by hand count toward SpawnCount, else each kind ends up one rack's worth plus extras.
        private static Dictionary<ItemDef, int> CountPlacedByHand()
        {
            var placed = new Dictionary<ItemDef, int>();
            foreach (var adapter in FindInScenes<ItemAdapter>())
            {
                if (adapter.GetComponentInParent<BakedItemLayout>(true) != null) continue;

                var id = new SerializedObject(adapter).FindProperty("def").FindPropertyRelative("id").stringValue;
                if (string.IsNullOrEmpty(id) || DefManager.GetDef<ItemDef>(id) is not { } def) continue;

                placed[def] = placed.TryGetValue(def, out var count) ? count + 1 : 1;
            }

            return placed;
        }

        private static List<T> FindInScenes<T>() where T : Component
        {
            var found = new List<T>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    found.AddRange(root.GetComponentsInChildren<T>());
                }
            }

            return found;
        }
    }

    [CustomEditor(typeof(BakedItemLayout))]
    public sealed class BakedItemLayoutEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var layout = (BakedItemLayout)target;
            EditorGUILayout.HelpBox($"{layout.transform.childCount} items. Bake again after changing item counts, " +
                                    "prefabs or scatter areas.", MessageType.Info);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Bake")) ItemLayoutBaker.Bake(layout);
                if (GUILayout.Button("Clear")) ItemLayoutBaker.Clear(layout);
            }
        }
    }
}
