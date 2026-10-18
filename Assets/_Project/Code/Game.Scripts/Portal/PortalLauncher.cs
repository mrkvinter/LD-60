using System;
using Code.Game.Core;
using Code.Game.Core.ExecutorSystem.Conditions;
using DG.Tweening;
using Game.Core.Contexts;
using UnityEngine;

namespace Game.Scripts.Portal
{
    /// <summary>
    /// Opens the portal when the machine restarts: the surface's Open goes from 0 to 1, the lights fade up with it,
    /// and the particles and the hum start. It opens on its own once the launch condition holds, or when a
    /// <see cref="Actions.LaunchPortalGameAction"/> runs (a button on the control panel, a trigger zone).
    /// </summary>
    public sealed class PortalLauncher : ObjectRegistry.MonoRegistered<PortalLauncher>, IConditionalItem
    {
        private static readonly int OpenId = Shader.PropertyToID("_Open");

        [Tooltip("Renderers with a Custom/PortalSurface material; their Open is driven from here.")]
        [SerializeField] private Renderer[] surfaces = Array.Empty<Renderer>();

        [Tooltip("Started when the portal opens. Turn off their Play On Awake.")]
        [SerializeField] private ParticleSystem[] particles = Array.Empty<ParticleSystem>();

        [Tooltip("Off while closed, then faded up to the intensity set on them in the scene.")]
        [SerializeField] private Light[] lights = Array.Empty<Light>();

        [SerializeField] private AudioSource hum;

        [SerializeField, Min(0f)] private float openSeconds = 2f;
        [SerializeField] private Ease openEase = Ease.OutCubic;

        [Tooltip("Opens as soon as this holds. Empty: only LaunchPortalGameAction opens it.")]
        [SerializeField] private ConditionJunction launchCondition;

        private MaterialPropertyBlock propertyBlock;
        private float[] lightIntensities = Array.Empty<float>();
        private float humVolume;
        private ConditionService conditionService;
        private Tween tween;
        private float open;

        // A condition that already holds when the scene loads (a loaded save) opens it at once, without the show.
        private bool isStarting;

        public ConditionJunction ConditionJunction => launchCondition;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            lightIntensities = new float[lights.Length];
            for (var i = 0; i < lights.Length; i++)
            {
                lightIntensities[i] = lights[i].intensity;
            }

            if (hum != null) humVolume = hum.volume;
            ShowClosed();
        }

        protected override void OnInitialize()
        {
            if (launchCondition.Conditions == null || launchCondition.Conditions.Length == 0) return;

            conditionService = GameServiceLocator.Resolve<ConditionService>();
            isStarting = true;
            conditionService.Subscribe(this);
            isStarting = false;
        }

        protected override void OnDispose()
        {
            conditionService?.Unsubscribe(this);
            tween?.Kill();
        }

        public void Activate() => Launch(isStarting);

        public void Deactivate() => Close();

        [ContextMenu("Launch")]
        public void Launch() => Launch(false);

        public void Launch(bool instant)
        {
            if (IsActive) return;

            IsActive = true;
            foreach (var light in lights)
            {
                light.enabled = true;
            }

            foreach (var system in particles)
            {
                system.Play(true);
            }

            if (hum != null) hum.Play();

            AnimateOpen(1f, instant ? 0f : openSeconds, null);
        }

        [ContextMenu("Close")]
        public void Close()
        {
            if (!IsActive) return;

            IsActive = false;
            foreach (var system in particles)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            AnimateOpen(0f, openSeconds, ShowClosed);
        }

        private void AnimateOpen(float target, float seconds, TweenCallback onComplete)
        {
            tween?.Kill();
            if (seconds <= 0f)
            {
                SetOpen(target);
                onComplete?.Invoke();
                return;
            }

            tween = DOTween.To(() => open, SetOpen, target, seconds)
                .SetEase(openEase)
                .SetLink(gameObject)
                .OnComplete(onComplete);
        }

        private void ShowClosed()
        {
            SetOpen(0f);
            foreach (var light in lights)
            {
                light.enabled = false;
            }

            foreach (var system in particles)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (hum != null) hum.Stop();
        }

        private void SetOpen(float value)
        {
            open = value;

            propertyBlock ??= new MaterialPropertyBlock();
            foreach (var surface in surfaces)
            {
                surface.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(OpenId, value);
                surface.SetPropertyBlock(propertyBlock);
            }

            for (var i = 0; i < lights.Length; i++)
            {
                lights[i].intensity = lightIntensities[i] * value;
            }

            if (hum != null) hum.volume = humVolume * value;
        }
    }
}
