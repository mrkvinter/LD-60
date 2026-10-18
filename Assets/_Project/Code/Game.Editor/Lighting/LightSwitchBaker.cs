using System;
using System.Collections.Generic;
using System.IO;
using Game.Scripts.Lighting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Code.Game.Editor.Lighting
{
    /// <summary>
    /// Bakes the open scenes twice, every <see cref="SwitchedLamp"/> off and then on, and keeps copies of both bakes
    /// for the <see cref="LightSwitch"/>. Unity overwrites its lightmaps on every bake, so the copies live in their own
    /// folder next to the scene. The scene is left baked with the lamps on.
    /// </summary>
    public static class LightSwitchBaker
    {
        private const string Menu = "Game/Lighting/Bake Light Switch (Off + On)";
        private const string OffName = "Off";
        private const string OnName = "On";

        // Lightmap indices from here up are Unity's "no lightmap" markers.
        private const int NoLightmap = 0xFFFE;

        // Unity may report a bake as completed only after Bake() has returned, so the switch's own bakes are told
        // apart by time too.
        private const double OwnBakeGrace = 5.0;

        private static bool isBaking;
        private static double ownBakeEndTime = double.NegativeInfinity;

        [InitializeOnLoadMethod]
        private static void WatchBakes() => Lightmapping.bakeCompleted += OnBakeCompleted;

        // Any other bake may pack the lightmaps differently, and the stored states then light the wrong spots.
        private static void OnBakeCompleted()
        {
            if (isBaking || EditorApplication.timeSinceStartup - ownBakeEndTime < OwnBakeGrace) return;
            if (Object.FindAnyObjectByType<LightSwitch>(FindObjectsInactive.Include) == null) return;

            Debug.LogWarning("[LightSwitch] The lighting was baked without the light switch: its off and on states " +
                             $"may no longer fit the lightmaps. Run {Menu}.");
        }

        [MenuItem(Menu)]
        private static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[LightSwitch] Bake outside Play mode.");
                return;
            }

            var switches = Object.FindObjectsByType<LightSwitch>(FindObjectsInactive.Include);
            if (switches.Length != 1)
            {
                Debug.LogError($"[LightSwitch] Expected one {nameof(LightSwitch)} in the open scenes, found " +
                               $"{switches.Length}.");
                return;
            }

            var lightSwitch = switches[0];
            var scene = lightSwitch.gameObject.scene;
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("[LightSwitch] Save the scene first: the bakes are kept next to it.", lightSwitch);
                return;
            }

            var lamps = Object.FindObjectsByType<SwitchedLamp>(FindObjectsInactive.Exclude);
            if (lamps.Length == 0)
            {
                Debug.LogError($"[LightSwitch] No {nameof(SwitchedLamp)} in the open scenes. Add it to the lamp " +
                               "prefab.", lightSwitch);
                return;
            }

            var sceneFolder = Path.GetDirectoryName(scene.path)?.Replace('\\', '/');
            var folder = EnsureFolder(sceneFolder, $"{scene.name}_LightSwitch");
            var authored = LampState.Capture(lamps);
            isBaking = true;
            try
            {
                if (!TryBake(lamps, false, folder, OffName, out var off, out var offLayout) ||
                    !TryBake(lamps, true, folder, OnName, out var on, out var onLayout))
                {
                    return;
                }

                if (off.LightmapCount != on.LightmapCount || !IsSameLayout(offLayout, onLayout))
                {
                    Debug.LogError("[LightSwitch] The two bakes packed the lightmaps differently, so they can't be " +
                                   "swapped. Did anything but the lamps change between them? Bake again.",
                        lightSwitch);
                    return;
                }

                Assign(lightSwitch, off, on);
                Debug.Log($"[LightSwitch] Baked {lamps.Length} lamps off and on: {on.LightmapCount} lightmaps and " +
                          $"{on.ProbeCount} probes each, kept in {folder}. Save the scene.", lightSwitch);
            }
            finally
            {
                isBaking = false;
                ownBakeEndTime = EditorApplication.timeSinceStartup;
                authored.Restore();
            }
        }

        private static bool TryBake(SwitchedLamp[] lamps, bool isOn, string folder, string name,
            out BakedLightingState state, out Dictionary<Renderer, (int, Vector4)> layout)
        {
            state = null;
            layout = null;

            foreach (var lamp in lamps)
            {
                lamp.SetOn(isOn);
            }

            if (!Lightmapping.Bake())
            {
                Debug.LogError($"[LightSwitch] The \"{name}\" bake failed or was cancelled.");
                return false;
            }

            state = Store(folder, name);
            layout = CaptureLayout();
            return true;
        }

        private static BakedLightingState Store(string folder, string name)
        {
            var lightmaps = LightmapSettings.lightmaps;
            var textureFolder = EnsureFolder(folder, name);
            var kept = new HashSet<string>();

            var colors = new Texture2D[lightmaps.Length];
            var directions = new Texture2D[lightmaps.Length];
            var shadowMasks = new Texture2D[lightmaps.Length];
            for (var i = 0; i < lightmaps.Length; i++)
            {
                colors[i] = Copy(lightmaps[i].lightmapColor, textureFolder, kept);
                directions[i] = Copy(lightmaps[i].lightmapDir, textureFolder, kept);
                shadowMasks[i] = Copy(lightmaps[i].shadowMask, textureFolder, kept);
            }

            // Left from an earlier bake that made more lightmaps.
            foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { textureFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!kept.Contains(path)) AssetDatabase.DeleteAsset(path);
            }

            var lightProbes = LightmapSettings.lightProbes;
            var probes = lightProbes != null ? lightProbes.bakedProbes : null;

            var statePath = $"{folder}/{name}.asset";
            var state = AssetDatabase.LoadAssetAtPath<BakedLightingState>(statePath);
            if (state == null)
            {
                state = ScriptableObject.CreateInstance<BakedLightingState>();
                AssetDatabase.CreateAsset(state, statePath);
            }

            state.Store(colors, directions, shadowMasks, probes ?? Array.Empty<SphericalHarmonicsL2>());
            EditorUtility.SetDirty(state);
            AssetDatabase.SaveAssetIfDirty(state);
            return state;
        }

        // Over an earlier copy the file is replaced in place, so the copy keeps its GUID and its importer settings.
        private static Texture2D Copy(Texture2D texture, string folder, HashSet<string> kept)
        {
            if (texture == null) return null;

            var from = AssetDatabase.GetAssetPath(texture);
            var to = $"{folder}/{Path.GetFileName(from)}";
            kept.Add(to);

            if (File.Exists(to))
            {
                File.Copy(from, to, true);
                AssetDatabase.ImportAsset(to, ImportAssetOptions.ForceUpdate);
            }
            else if (!AssetDatabase.CopyAsset(from, to))
            {
                Debug.LogError($"[LightSwitch] Couldn't copy {from} to {to}.");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(to);
        }

        private static Dictionary<Renderer, (int, Vector4)> CaptureLayout()
        {
            var layout = new Dictionary<Renderer, (int, Vector4)>();
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (renderer.lightmapIndex is >= 0 and < NoLightmap)
                {
                    layout[renderer] = (renderer.lightmapIndex, renderer.lightmapScaleOffset);
                }
            }

            return layout;
        }

        private static bool IsSameLayout(Dictionary<Renderer, (int, Vector4)> a, Dictionary<Renderer, (int, Vector4)> b)
        {
            if (a.Count != b.Count) return false;

            foreach (var (renderer, place) in a)
            {
                if (!b.TryGetValue(renderer, out var other) || other != place) return false;
            }

            return true;
        }

        private static void Assign(LightSwitch lightSwitch, BakedLightingState off, BakedLightingState on)
        {
            var serialized = new SerializedObject(lightSwitch);
            serialized.FindProperty("off").objectReferenceValue = off;
            serialized.FindProperty("on").objectReferenceValue = on;
            serialized.ApplyModifiedProperties();
        }

        private static string EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);

            return path;
        }

        // The lamps as they were before the bakes, so switching them for the bakes leaves nothing behind in the scene.
        private sealed class LampState
        {
            private readonly List<(Light light, bool isEnabled)> lights = new();
            private readonly List<(Renderer renderer, Material[] materials)> renderers = new();

            public static LampState Capture(SwitchedLamp[] lamps)
            {
                var state = new LampState();
                foreach (var lamp in lamps)
                {
                    foreach (var light in lamp.GetComponentsInChildren<Light>(true))
                    {
                        state.lights.Add((light, light.enabled));
                    }

                    foreach (var renderer in lamp.GetComponentsInChildren<Renderer>(true))
                    {
                        state.renderers.Add((renderer, renderer.sharedMaterials));
                    }
                }

                return state;
            }

            public void Restore()
            {
                foreach (var (light, isEnabled) in lights)
                {
                    if (light != null) light.enabled = isEnabled;
                }

                foreach (var (renderer, materials) in renderers)
                {
                    if (renderer != null) renderer.sharedMaterials = materials;
                }
            }
        }
    }
}
