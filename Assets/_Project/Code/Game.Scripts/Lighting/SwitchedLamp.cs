using UnityEngine;

namespace Game.Scripts.Lighting
{
    /// <summary>
    /// A lamp that <see cref="LightSwitch"/> turns on and off: every light under it, and optionally its glowing
    /// material swapped for a dark one. Goes on the lamp prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwitchedLamp : MonoBehaviour
    {
        // Without both only the lights switch.
        [SerializeField] private Material onMaterial;
        [SerializeField] private Material offMaterial;

        private Light[] lights;
        private Renderer[] renderers;

        public void SetOn(bool isOn)
        {
            lights ??= GetComponentsInChildren<Light>(true);
            foreach (var source in lights)
            {
                source.enabled = isOn;
            }

            if (onMaterial == null || offMaterial == null) return;

            renderers ??= GetComponentsInChildren<Renderer>(true);
            var from = isOn ? offMaterial : onMaterial;
            var to = isOn ? onMaterial : offMaterial;
            foreach (var target in renderers)
            {
                var materials = target.sharedMaterials;
                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != from) continue;

                    materials[i] = to;
                    changed = true;
                }

                if (changed) target.sharedMaterials = materials;
            }
        }
    }
}
