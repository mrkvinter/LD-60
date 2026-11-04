using System;
using DG.Tweening;
using Game.Scripts.Player;
using UnityEngine;

namespace Game.Scripts.UI
{
    public sealed class ThanksScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup screen;
        [SerializeField, Min(0f)] private float fadeTime = 1f;
        [SerializeField, Min(0f)] private float holdTime = 3f;

        private Sequence sequence;

        private void Awake()
        {
            screen.alpha = 0f;
        }

        private void OnDestroy()
        {
            sequence?.Kill();
        }

        // Without a reveal the screen stays black and the player stays held: a restart takes it from there.
        public void Show(Action whileBlack, bool reveal)
        {
            var player = FindAnyObjectByType<PlayerAdapter>();
            var input = player != null ? player.Entity?.GetComponent<PlayerInputComponent>() : null;
            if (input != null) input.IsBlocked = true;

            sequence?.Kill();
            sequence = DOTween.Sequence()
                .Append(Fade(1f))
                .AppendInterval(holdTime)
                .AppendCallback(() => whileBlack());

            if (!reveal) return;

            sequence.Append(Fade(0f)).AppendCallback(() =>
            {
                if (input != null) input.IsBlocked = false;
            });
        }

        private Tween Fade(float alpha) => DOTween.To(() => screen.alpha, value => screen.alpha = value, alpha, fadeTime);
    }
}
