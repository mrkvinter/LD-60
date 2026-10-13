using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.UI.Animations
{
    public class FadeInDitherWidgetAnimation : BaseWidgetAnimation
    {
        [SerializeField] private float duration;
        [SerializeField] private Vector2 offset;
        [SerializeField] private Material ditherMaterial;
        [SerializeField] private Material[] preparedMaterials;

        private Material ditherMaterialInstance;
        private List<Material> fontMaterials;
        private Vector2 closedPosition;
        private bool isOpen;
        private Tween positionTween;
        private Tween ditherTween;
        private static readonly int DitherIntensity = Shader.PropertyToID("_DitherIntensity");


        protected override void OnInitialize()
        {
            closedPosition = Layout.RectTransform.anchoredPosition;
            ditherMaterialInstance = new Material(ditherMaterial);
            fontMaterials = new List<Material>();
            fontMaterials.AddRange(preparedMaterials);
            foreach (var preparedMaterial in preparedMaterials)
            {
                preparedMaterial.SetFloat(DitherIntensity, 1);
            }

            var tmpTexts = GetComponentsInChildren<TMP_Text>();
            foreach (var text in tmpTexts)
            {
                text.fontMaterial = new Material(text.fontMaterial);
                fontMaterials.Add(text.fontMaterial);
            }
            var texts = GetComponentsInChildren<UnityEngine.UI.Text>();
            foreach (var text in texts)
            {
                text.material = new Material(text.material);
                fontMaterials.Add(text.material);
            }

            var images = GetComponentsInChildren<UnityEngine.UI.Image>();
            foreach (var image in images)
            {
                image.material = ditherMaterialInstance;
            }
            
            var sprites = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sprite in sprites)
            {
                sprite.material = ditherMaterialInstance;
            }
        }

        protected override void PlayOpen(bool instant, Action callback)
        {
            var settledClosed = !isOpen && !IsAnimating;
            KillTweens();

            if (settledClosed)
            {
                closedPosition = Layout.RectTransform.anchoredPosition;
            }

            isOpen = true;
            var targetPosition = closedPosition + offset;

            if (instant)
            {
                ditherMaterialInstance.SetFloat(DitherIntensity, 1);
                SetDitherIntensity(1);
                Layout.RectTransform.anchoredPosition = targetPosition;
                callback.Invoke();
                return;
            }

            // Linked to the layout: widgets parented to a scene canvas die with the scene,
            // and a tween on a destroyed RectTransform corrupts the whole TweenManager.
            ditherTween = DOTween.To(() => ditherMaterialInstance.GetFloat(DitherIntensity),
                SetDitherIntensity, 1, duration).SetLink(Layout.gameObject);
            positionTween = Layout.RectTransform.DOAnchorPos(targetPosition, duration)
                .OnComplete(callback.Invoke)
                .SetLink(Layout.gameObject);
        }

        protected override void PlayClose(bool instant, Action callback)
        {
            KillTweens();
            isOpen = false;

            if (instant)
            {
                ditherMaterialInstance.SetFloat(DitherIntensity, 0);
                SetDitherIntensity(0);
                Layout.RectTransform.anchoredPosition = closedPosition;
                callback.Invoke();
                return;
            }

            ditherTween = DOTween.To(() => ditherMaterialInstance.GetFloat(DitherIntensity),
                SetDitherIntensity, 0, duration).SetLink(Layout.gameObject);
            positionTween = Layout.RectTransform.DOAnchorPos(closedPosition, duration)
                .OnComplete(callback.Invoke)
                .SetLink(Layout.gameObject);
        }

        private bool IsAnimating => positionTween != null && positionTween.IsActive();

        private void KillTweens()
        {
            positionTween?.Kill();
            ditherTween?.Kill();
            positionTween = null;
            ditherTween = null;
        }

        private void SetDitherIntensity(float value)
        {
            ditherMaterialInstance.SetFloat(DitherIntensity, value);
            foreach (var fontMaterial in fontMaterials)
            {
                fontMaterial.SetFloat(DitherIntensity, value);
            }
        }
    }
}