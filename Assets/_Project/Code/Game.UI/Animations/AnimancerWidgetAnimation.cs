using System;
using Animancer;
using UnityEngine;

namespace Game.UI.Animations
{
    public class AnimancerWidgetAnimation : BaseWidgetAnimation
    {
        [SerializeField] private AnimancerComponent animancerComponent;
        [SerializeField] private AnimationClip openClip;
        [SerializeField] private AnimationClip closeClip;

        protected override void PlayOpen(bool instant, Action callback)
        {
            var state = animancerComponent.Play(openClip);
            
            if (instant)
            {
                state.NormalizedTime = 1;
                state.Stop();
                callback?.Invoke();
            }
            else
            {
                state.Events(this).OnEnd = () =>
                {
                    state.Stop();
                    callback?.Invoke();
                };
            }
        }

        protected override void PlayClose(bool instant, Action callback)
        {
            var state = animancerComponent.Play(closeClip);
            
            if (instant)
            {
                state.NormalizedTime = 1;
                state.Stop();
                callback?.Invoke();
            }
            else
            {
                state.Events(this).OnEnd = () =>
                {
                    state.Stop();
                    callback?.Invoke();
                };
            }
        }
    }
}