using System;
using System.Collections.Generic;
using System.Text;
using Code.Game.Scripts.EntitySystem;
using Game.Core.Contexts;
using RG.DefinitionSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Construction
{
    /// <summary>
    /// Keeps <see cref="built"/> off until the site gets its resources. Until then a see-through copy of it stands in
    /// its place: no solid colliders, only a trigger the hands aim at, and a label with what is still missing.
    /// </summary>
    public sealed class ConstructionSiteAdapter : EntityAdapter<ConstructionSite>
    {
        [SerializeField] private DefRef<BuildingDef> building;

        [Tooltip("The real object. Turned off on start, turned on once built; its meshes make the ghost.")]
        [SerializeField] private GameObject built;

        private ConstructionSettingsDef settings;
        private GameObject ghost;
        private TextMeshPro label;
        private Camera viewCamera;

        // Before any Start, so whatever the built object holds (a rack, say) registers only once it is built.
        private void Awake()
        {
            if (built == null)
            {
                Debug.LogError($"{nameof(ConstructionSiteAdapter)} on '{name}' has nothing to build.", this);
                return;
            }

            if (transform.IsChildOf(built.transform))
            {
                Debug.LogError($"{nameof(ConstructionSiteAdapter)} on '{name}' sits inside what it builds.", this);
                return;
            }

            built.SetActive(false);
        }

        protected override ConstructionSite CreateEntity()
        {
            if (building.IsEmpty)
            {
                throw new InvalidOperationException($"{nameof(ConstructionSiteAdapter)} on '{name}' has no building def.");
            }

            var def = DefManager.GetDef(building);
            var requirements = new List<BuildRequirement>();
            foreach (var cost in def.Cost)
            {
                requirements.Add(new BuildRequirement(DefManager.GetDef(cost.Resource), cost.Count));
            }

            var site = new ConstructionSite(def.Name, requirements);
            site.Changed += Refresh;
            site.Built += ShowBuilt;
            return site;
        }

        protected override void OnEntityInitialized()
        {
            settings = DefManager.GetSingletonDef<ConstructionSettingsDef>();
            GameServiceLocator.Resolve<ConstructionProgress>().Register(DefManager.GetDef(building), Entity);

            if (Entity.IsBuilt || built == null)
            {
                ShowBuilt();
                return;
            }

            var bounds = CreateGhost();
            CreateLabel(bounds);
            Refresh();
        }

        private void Update()
        {
            if (label == null) return;

            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            var labelTransform = label.transform;
            var offset = labelTransform.position - viewCamera.transform.position;
            label.enabled = offset.sqrMagnitude <= settings.LabelDistance * settings.LabelDistance;
            if (label.enabled) labelTransform.rotation = Quaternion.LookRotation(offset);
        }

        private void ShowBuilt()
        {
            if (ghost != null) Destroy(ghost);
            if (label != null) Destroy(label.gameObject);
            ghost = null;
            label = null;

            if (built != null) built.SetActive(true);
        }

        private void Refresh()
        {
            if (label == null) return;

            var doneColor = ColorUtility.ToHtmlStringRGB(settings.DoneColor);
            var text = new StringBuilder();
            text.Append("<b>").Append(Entity.Name).Append("</b>");
            for (var i = 0; i < Entity.Requirements.Count; i++)
            {
                var requirement = Entity.Requirements[i];
                var delivered = Entity.Delivered[i];
                var line = $"{requirement.Resource.Name}: {delivered}/{requirement.Count}";
                text.Append('\n').Append(delivered >= requirement.Count ? $"<color=#{doneColor}>{line}</color>" : line);
            }

            label.text = text.ToString();
        }

        /// <summary>Copies the built object's meshes in the ghost material; returns their bounds in site space.</summary>
        private Bounds CreateGhost()
        {
            ghost = new GameObject("Ghost");
            ghost.transform.SetParent(transform, false);

            var toGhost = ghost.transform.worldToLocalMatrix;
            var bounds = new Bounds();
            var hasBounds = false;

            foreach (var source in built.GetComponentsInChildren<MeshFilter>(true))
            {
                if (source.sharedMesh == null || !source.TryGetComponent<MeshRenderer>(out var sourceRenderer)) continue;

                var matrix = toGhost * source.transform.localToWorldMatrix;
                var part = new GameObject(source.name);
                part.transform.SetParent(ghost.transform, false);
                part.transform.SetLocalPositionAndRotation(matrix.GetPosition(), matrix.rotation);
                part.transform.localScale = matrix.lossyScale;

                part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var partRenderer = part.AddComponent<MeshRenderer>();
                var materials = new Material[sourceRenderer.sharedMaterials.Length];
                Array.Fill(materials, settings.GhostMaterial);
                partRenderer.sharedMaterials = materials;
                partRenderer.shadowCastingMode = ShadowCastingMode.Off;

                var meshBounds = source.sharedMesh.bounds;
                for (var corner = 0; corner < 8; corner++)
                {
                    var point = matrix.MultiplyPoint3x4(meshBounds.center + Vector3.Scale(meshBounds.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                    if (hasBounds) bounds.Encapsulate(point);
                    else bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
            }

            var trigger = ghost.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = bounds.center;
            trigger.size = bounds.size;

            return bounds;
        }

        private void CreateLabel(Bounds ghostBounds)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.position =
                ghost.transform.TransformPoint(new Vector3(ghostBounds.center.x, ghostBounds.max.y, ghostBounds.center.z)) +
                Vector3.up * settings.LabelHeight;

            label = labelObject.AddComponent<TextMeshPro>();
            label.fontSize = settings.LabelFontSize;
            label.alignment = TextAlignmentOptions.Bottom;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(6f, 2f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
        }
    }
}
