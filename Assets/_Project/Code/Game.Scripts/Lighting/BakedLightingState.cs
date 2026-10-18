using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Lighting
{
    /// <summary>One bake of the scene for <see cref="LightSwitch"/>: copies of its lightmaps and its light probes.</summary>
    public sealed class BakedLightingState : ScriptableObject
    {
        private const int Channels = 3;
        private const int Coefficients = 9;
        private const int FloatsPerProbe = Channels * Coefficients;

        // One entry per lightmap; directions and shadow masks are null where the bake made none.
        [SerializeField] private Texture2D[] colors = Array.Empty<Texture2D>();
        [SerializeField] private Texture2D[] directions = Array.Empty<Texture2D>();
        [SerializeField] private Texture2D[] shadowMasks = Array.Empty<Texture2D>();

        // Every probe's red, green and blue coefficients in a row. Hidden: thousands of numbers stall the Inspector.
        [SerializeField, HideInInspector] private float[] probes = Array.Empty<float>();

        public int LightmapCount => colors.Length;
        public int ProbeCount => probes.Length / FloatsPerProbe;

        public void Store(Texture2D[] colors, Texture2D[] directions, Texture2D[] shadowMasks,
            SphericalHarmonicsL2[] probes)
        {
            this.colors = colors;
            this.directions = directions;
            this.shadowMasks = shadowMasks;

            this.probes = new float[probes.Length * FloatsPerProbe];
            for (var probe = 0; probe < probes.Length; probe++)
            {
                for (var channel = 0; channel < Channels; channel++)
                {
                    for (var coefficient = 0; coefficient < Coefficients; coefficient++)
                    {
                        this.probes[Index(probe, channel, coefficient)] = probes[probe][channel, coefficient];
                    }
                }
            }
        }

        public LightmapData[] CreateLightmaps()
        {
            var lightmaps = new LightmapData[colors.Length];
            for (var i = 0; i < lightmaps.Length; i++)
            {
                lightmaps[i] = new LightmapData
                {
                    lightmapColor = colors[i],
                    lightmapDir = At(directions, i),
                    shadowMask = At(shadowMasks, i)
                };
            }

            return lightmaps;
        }

        public SphericalHarmonicsL2[] CreateProbes()
        {
            var result = new SphericalHarmonicsL2[ProbeCount];
            for (var probe = 0; probe < result.Length; probe++)
            {
                for (var channel = 0; channel < Channels; channel++)
                {
                    for (var coefficient = 0; coefficient < Coefficients; coefficient++)
                    {
                        result[probe][channel, coefficient] = probes[Index(probe, channel, coefficient)];
                    }
                }
            }

            return result;
        }

        private static int Index(int probe, int channel, int coefficient) =>
            probe * FloatsPerProbe + channel * Coefficients + coefficient;

        private static Texture2D At(Texture2D[] textures, int index) => index < textures.Length ? textures[index] : null;
    }
}
