using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Lighting
{
    /// <summary>
    /// Switches the scene between two bakes, lamps off and lamps on, made by Game/Lighting/Bake Light Switch: swaps
    /// the lightmaps and the light probes and turns every <see cref="SwitchedLamp"/>. One per scene, since the
    /// lightmaps belong to the whole scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LightSwitch : MonoBehaviour
    {
        // Too small to see; see RefreshProbeLighting.
        private const float AmbientNudge = 1e-5f;

        [SerializeField] private BakedLightingState off;
        [SerializeField] private BakedLightingState on;
        [SerializeField] private bool startOn;

        [Tooltip("Seconds of each blink when the light comes on, starting lit: lit, dark, lit... After the last one " +
                 "it stays lit. Empty: no blinking.")]
        [SerializeField] private float[] flicker = Array.Empty<float>();

        private LightmapData[] offLightmaps;
        private LightmapData[] onLightmaps;
        private SphericalHarmonicsL2[] offProbes;
        private SphericalHarmonicsL2[] onProbes;
        private SwitchedLamp[] lamps;

        // What the scene loaded with, put back when the switch goes away: in the Editor the probes are the scene's
        // lighting data asset, and swapping them in Play mode would outlive it.
        private LightmapData[] sceneLightmaps;
        private SphericalHarmonicsL2[] sceneProbes;
        private SphericalHarmonicsL2 ambientProbe;

        private bool isReady;
        private bool isNudged;
        private Coroutine flickering;

        public bool IsOn { get; private set; }

        /// <summary>Raised once per switch, as it starts: blinking doesn't raise it again.</summary>
        public event Action<bool> Switched;

        private void Start()
        {
            isReady = TryPrepare();
            if (!isReady) return;

            IsOn = startOn;
            Show(IsOn);
        }

        private void OnDestroy()
        {
            if (!isReady) return;

            LightmapSettings.lightmaps = sceneLightmaps;
            SetProbes(sceneProbes);
            RenderSettings.ambientProbe = ambientProbe;
        }

        [ContextMenu("Toggle")]
        public void Toggle() => SetOn(!IsOn);

        public void SetOn(bool isOn)
        {
            if (!isReady || isOn == IsOn) return;

            IsOn = isOn;
            if (flickering != null)
            {
                StopCoroutine(flickering);
                flickering = null;
            }

            if (isOn && flicker.Length > 0)
            {
                flickering = StartCoroutine(Flicker());
            }
            else
            {
                Show(isOn);
            }

            Switched?.Invoke(isOn);
        }

        private bool TryPrepare()
        {
            if (off == null || on == null)
            {
                Debug.LogError("[LightSwitch] No baked states. Run Game/Lighting/Bake Light Switch.", this);
                return false;
            }

            sceneLightmaps = LightmapSettings.lightmaps;
            var lightProbes = LightmapSettings.lightProbes;
            var probeCount = lightProbes != null ? lightProbes.count : 0;

            // A plain bake after the switch's one may have packed the lightmaps differently; only the counts can be
            // checked here.
            if (off.LightmapCount != sceneLightmaps.Length || on.LightmapCount != sceneLightmaps.Length ||
                off.ProbeCount != probeCount || on.ProbeCount != probeCount)
            {
                Debug.LogError($"[LightSwitch] The scene has {sceneLightmaps.Length} lightmaps and {probeCount} " +
                               $"probes, the off state {off.LightmapCount} and {off.ProbeCount}, the on state " +
                               $"{on.LightmapCount} and {on.ProbeCount}. Run Game/Lighting/Bake Light Switch " +
                               "again.", this);
                return false;
            }

            sceneProbes = probeCount > 0 ? lightProbes.bakedProbes : Array.Empty<SphericalHarmonicsL2>();
            ambientProbe = RenderSettings.ambientProbe;

            offLightmaps = off.CreateLightmaps();
            onLightmaps = on.CreateLightmaps();
            offProbes = off.CreateProbes();
            onProbes = on.CreateProbes();
            lamps = FindObjectsByType<SwitchedLamp>(FindObjectsInactive.Include);
            return true;
        }

        private IEnumerator Flicker()
        {
            var isLit = true;
            foreach (var seconds in flicker)
            {
                Show(isLit);
                yield return new WaitForSeconds(seconds);
                isLit = !isLit;
            }

            Show(true);
            flickering = null;
        }

        private void Show(bool isLit)
        {
            LightmapSettings.lightmaps = isLit ? onLightmaps : offLightmaps;
            SetProbes(isLit ? onProbes : offProbes);

            foreach (var lamp in lamps)
            {
                if (lamp != null) lamp.SetOn(isLit);
            }

            RefreshProbeLighting();
        }

        // The probes of an unloading scene may already be gone; a count that doesn't match would throw.
        private static void SetProbes(SphericalHarmonicsL2[] probes)
        {
            var lightProbes = LightmapSettings.lightProbes;
            if (probes.Length > 0 && lightProbes != null && lightProbes.count == probes.Length)
            {
                lightProbes.bakedProbes = probes;
            }
        }

        // The GPU Resident Drawer reads the probes again for objects that don't move only when the ambient probe
        // changes, so with the probes alone the items lying around would keep their old light. Each switch flips the
        // ambient probe between its own value and one nudged by an amount too small to see.
        private void RefreshProbeLighting()
        {
            isNudged = !isNudged;
            var probe = ambientProbe;
            if (isNudged) probe[0, 0] += AmbientNudge;

            RenderSettings.ambientProbe = probe;
        }
    }
}
