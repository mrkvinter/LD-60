using System;
using System.Collections;
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
        private const float SwapScale = 0.01f;
        private const float MaxFlightTime = 3f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private DefRef<ItemDef> def;

        [Tooltip("Highlighted and copied into the placement ghost. Empty means every renderer under the item.")]
        [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();

        [SerializeField] private Collider pickCollider;
        [SerializeField] private Rigidbody body;

        private MaterialPropertyBlock propertyBlock;
        private Color[] baseColors = Array.Empty<Color>();
        private Material[][] baseMaterials = Array.Empty<Material[]>();
        private Vector3 baseScale = Vector3.one;
        private bool isVisible = true;
        private bool isHighlighted;
        private bool isMarked;
        private Coroutine flight;
        private Bounds? localBounds;

        public Bounds LocalBounds => localBounds ??= CaptureLocalBounds(transform.localScale);

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
            localBounds = CaptureLocalBounds(baseScale);
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
            isHighlighted = highlighted;
            ApplyTint();
        }

        void IItemView.SetMarked(bool marked)
        {
            if (isMarked == marked) return;

            isMarked = marked;
            var mark = DefManager.GetSingletonDef<ItemSettingsDef>().MarkMaterial;
            for (var i = 0; i < renderers.Length; i++)
            {
                var target = renderers[i];
                if (target == null) continue;

                if (!marked || mark == null)
                {
                    target.sharedMaterials = baseMaterials[i];
                    continue;
                }

                var materials = new Material[baseMaterials[i].Length + 1];
                baseMaterials[i].CopyTo(materials, 0);
                materials[^1] = mark;
                target.sharedMaterials = materials;
            }
        }

        private void ApplyTint()
        {
            var highlight = DefManager.GetSingletonDef<ItemSettingsDef>().HighlightColor;
            for (var i = 0; i < renderers.Length; i++)
            {
                var target = renderers[i];
                if (target == null) continue;

                if (!isHighlighted)
                {
                    target.SetPropertyBlock(null);
                    continue;
                }

                propertyBlock ??= new MaterialPropertyBlock();
                propertyBlock.SetColor(BaseColorId, Color.Lerp(baseColors[i], highlight, HighlightBlend));
                target.SetPropertyBlock(propertyBlock);
            }
        }

        void IItemView.SetVisible(bool visible)
        {
            SetRenderersEnabled(visible);
        }

        void IItemView.SetMode(ItemMode mode)
        {
            if (pickCollider != null) pickCollider.enabled = mode is ItemMode.Loose or ItemMode.Stored;
            if (body == null) return;

            StopFlight();
            SetPrecise(false);
            body.isKinematic = mode != ItemMode.Loose;
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

        void IItemView.SwapIn(Transform parent, Vector3 localPosition, Quaternion localRotation, float scale,
            Vector3 fromLocalPosition)
        {
            transform.SetParent(parent, true);
            transform.DOKill();

            if (!isVisible)
            {
                transform.SetLocalPositionAndRotation(fromLocalPosition, localRotation);
                transform.localScale = baseScale * SwapScale;
                SetRenderersEnabled(true);
            }

            var duration = DefManager.GetSingletonDef<ItemSettingsDef>().SwapDuration;
            transform.DOLocalMove(localPosition, duration).SetEase(Ease.OutCubic);
            transform.DOLocalRotateQuaternion(localRotation, duration).SetEase(Ease.OutCubic);
            transform.DOScale(baseScale * scale, duration).SetEase(Ease.OutCubic);
        }

        void IItemView.SwapOut(Vector3 toLocalPosition)
        {
            transform.DOKill();

            var duration = DefManager.GetSingletonDef<ItemSettingsDef>().SwapDuration;
            transform.DOLocalMove(toLocalPosition, duration).SetEase(Ease.InCubic);
            transform.DOScale(baseScale * SwapScale, duration).SetEase(Ease.InCubic)
                .OnComplete(() => SetRenderersEnabled(false));
        }

        void IItemView.Release(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            transform.DOKill();
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);

            if (transform.localScale != baseScale)
            {
                var duration = DefManager.GetSingletonDef<ItemSettingsDef>().FlightDuration;
                transform.DOScale(baseScale, duration).SetEase(Ease.OutCubic);
            }

            if (body == null) return;

            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = velocity;
            body.angularVelocity = velocity == Vector3.zero ? Vector3.zero : Random.insideUnitSphere * ReleaseSpin;

            StopFlight();
            flight = StartCoroutine(TrackFlight());
        }

        void IItemView.Settle()
        {
            if (body == null) return;

            StopFlight();
            SetPrecise(false);
            body.Sleep();
        }

        void IItemView.Consume()
        {
            transform.DOKill();
            GameServiceLocator.Resolve<SortingProgress>().Unregister(Entity);
            Destroy();
        }

        private IEnumerator TrackFlight()
        {
            SetPrecise(true);

            var landBy = Time.time + MaxFlightTime;
            var wait = new WaitForFixedUpdate();
            do
            {
                yield return wait;
            } while (!body.IsSleeping() && Time.time < landBy);

            SetPrecise(false);
            flight = null;
        }

        private void StopFlight()
        {
            if (flight == null) return;

            StopCoroutine(flight);
            flight = null;
        }

        private void SetPrecise(bool precise)
        {
            body.collisionDetectionMode = precise
                ? CollisionDetectionMode.ContinuousSpeculative
                : CollisionDetectionMode.Discrete;
            body.interpolation = precise ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        }

        private Bounds CaptureLocalBounds(Vector3 scale)
        {
            if (pickCollider == null) return default;

            var box = pickCollider switch
            {
                BoxCollider b => new Bounds(b.center, b.size),
                SphereCollider s => new Bounds(s.center, Vector3.one * (2f * s.radius)),
                CapsuleCollider c => new Bounds(c.center, CapsuleSize(c)),
                MeshCollider { sharedMesh: { } mesh } => mesh.bounds,
                _ => default
            };

            var toRoot = transform.worldToLocalMatrix * pickCollider.transform.localToWorldMatrix;
            var result = new Bounds(toRoot.MultiplyPoint3x4(box.center), Vector3.zero);
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2f - Vector3.one;
                result.Encapsulate(toRoot.MultiplyPoint3x4(box.center + Vector3.Scale(box.extents, corner)));
            }

            return new Bounds(Vector3.Scale(result.center, scale), Vector3.Scale(result.size, scale));
        }

        private static Vector3 CapsuleSize(CapsuleCollider capsule)
        {
            var size = Vector3.one * (2f * capsule.radius);
            size[capsule.direction] = Mathf.Max(capsule.height, size.x);
            return size;
        }

        private void SetRenderersEnabled(bool visible)
        {
            isVisible = visible;
            foreach (var target in renderers)
            {
                if (target != null) target.enabled = visible;
            }
        }

        private void CaptureColors()
        {
            if (renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);

            baseColors = new Color[renderers.Length];
            baseMaterials = new Material[renderers.Length][];
            for (var i = 0; i < renderers.Length; i++)
            {
                baseMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterials : Array.Empty<Material>();
                var material = renderers[i] != null ? renderers[i].sharedMaterial : null;
                baseColors[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }
        }
    }
}
