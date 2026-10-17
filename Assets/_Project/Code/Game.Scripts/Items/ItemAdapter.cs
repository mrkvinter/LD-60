using System;
using Code.Game.Scripts.EntitySystem;
using DG.Tweening;
using Game.Core.Contexts;
using RG.DefinitionSystem.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Game.Scripts.Items
{
    public sealed class ItemAdapter : EntityAdapter<BaseItem>, IItemView
    {
        private const float HighlightBlend = 0.6f;
        private const float ReleaseSpin = 2f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private DefRef<ItemDef> def;

        [Tooltip("Highlighted and copied into the placement ghost. Empty means every renderer under the item.")]
        [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();

        [SerializeField] private Collider pickCollider;
        [SerializeField] private Rigidbody body;

        private MaterialPropertyBlock propertyBlock;
        private Color[] baseColors = Array.Empty<Color>();
        private Vector3 baseScale = Vector3.one;

        public void Setup(DefRef<ItemDef> itemDef)
        {
            def = itemDef;
        }

        protected override BaseItem CreateEntity()
        {
            if (def.IsEmpty)
            {
                throw new InvalidOperationException($"{nameof(ItemAdapter)} on '{name}' has no item def.");
            }

            baseScale = transform.localScale;
            CaptureColors();
            return DefManager.GetDef(def).CreateItem(this);
        }

        protected override void OnEntityInitialized()
        {
            GameServiceLocator.Resolve<SortingProgress>().Register(Entity);
        }

        public GameObject CreateGhost(Material material)
        {
            var ghost = new GameObject($"{name} Ghost");
            ghost.transform.localScale = baseScale;
            var toRoot = transform.worldToLocalMatrix;

            foreach (var source in renderers)
            {
                if (source == null || !source.TryGetComponent<MeshFilter>(out var filter)) continue;

                var part = new GameObject(source.name);
                var matrix = toRoot * source.transform.localToWorldMatrix;
                part.transform.SetParent(ghost.transform, false);
                part.transform.SetLocalPositionAndRotation(matrix.GetPosition(), matrix.rotation);
                part.transform.localScale = matrix.lossyScale;

                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var meshRenderer = part.AddComponent<MeshRenderer>();
                var materials = new Material[source.sharedMaterials.Length];
                Array.Fill(materials, material);
                meshRenderer.sharedMaterials = materials;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            return ghost;
        }

        void IItemView.SetHighlighted(bool highlighted)
        {
            var highlight = DefManager.GetSingletonDef<ItemSettingsDef>().HighlightColor;
            for (var i = 0; i < renderers.Length; i++)
            {
                var target = renderers[i];
                if (target == null) continue;

                if (!highlighted)
                {
                    target.SetPropertyBlock(null);
                    continue;
                }

                propertyBlock ??= new MaterialPropertyBlock();
                propertyBlock.SetColor(BaseColorId, Color.Lerp(baseColors[i], highlight, HighlightBlend));
                target.SetPropertyBlock(propertyBlock);
            }
        }

        void IItemView.SetMode(ItemMode mode)
        {
            if (pickCollider != null) pickCollider.enabled = mode is ItemMode.Loose or ItemMode.Stored;
            if (body == null) return;

            if (mode == ItemMode.Loose)
            {
                body.isKinematic = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
            else
            {
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.interpolation = RigidbodyInterpolation.None;
                body.isKinematic = true;
            }
        }

        void IItemView.FlyTo(Transform parent, Vector3 localPosition, Quaternion localRotation, float scale, bool instant)
        {
            transform.SetParent(parent, true);
            transform.DOKill();

            var localScale = baseScale * scale;
            if (instant)
            {
                transform.SetLocalPositionAndRotation(localPosition, localRotation);
                transform.localScale = localScale;
                return;
            }

            var duration = DefManager.GetSingletonDef<ItemSettingsDef>().FlightDuration;
            transform.DOLocalMove(localPosition, duration).SetEase(Ease.OutCubic);
            transform.DOLocalRotateQuaternion(localRotation, duration).SetEase(Ease.OutCubic);
            transform.DOScale(localScale, duration).SetEase(Ease.OutCubic);
        }

        void IItemView.Release(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            transform.DOKill();
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);

            var duration = DefManager.GetSingletonDef<ItemSettingsDef>().FlightDuration;
            transform.DOScale(baseScale, duration).SetEase(Ease.OutCubic);

            if (body == null) return;

            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = velocity;
            body.angularVelocity = velocity == Vector3.zero ? Vector3.zero : Random.insideUnitSphere * ReleaseSpin;
        }

        private void CaptureColors()
        {
            if (renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);

            baseColors = new Color[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                var material = renderers[i] != null ? renderers[i].sharedMaterial : null;
                baseColors[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }
        }
    }
}
