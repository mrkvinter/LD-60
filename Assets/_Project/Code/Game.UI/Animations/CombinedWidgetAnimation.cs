using System;
using UnityEngine;

namespace Game.UI.Animations
{
    public class CombinedWidgetAnimation : BaseWidgetAnimation
    {
        [SerializeField] private BaseWidgetAnimation[] animations;

        protected override void OnInitialize()
        {
            foreach (var widgetAnimation in animations)
            {
                widgetAnimation.Initialize(Layout);
            }
        }

        protected override void PlayOpen(bool instant, Action callback)
        {
            PlayAnimations(WidgetAnimationType.Open, instant, callback);
        }

        protected override void PlayClose(bool instant, Action callback)
        {
            PlayAnimations(WidgetAnimationType.Close, instant, callback);
        }

        private void PlayAnimations(WidgetAnimationType widgetAnimationType, bool instant, Action callback)
        {
            var animationsCount = animations.Length;
            var animationsPlayed = 0;

            void OnAnimationEnded()
            {
                animationsPlayed++;

                if (animationsPlayed == animationsCount)
                {
                    callback?.Invoke();
                }
            }

            foreach (var widgetAnimation in animations)
            {
                widgetAnimation.Play(widgetAnimationType, instant, OnAnimationEnded);
            }
        }
    }
}