using System;
using Game.UI.Base;
using UnityEngine;

namespace Game.UI.Animations
{
    public abstract class BaseWidgetAnimation : MonoBehaviour, IWidgetAnimation
    {
        private event Action OnAnimationEnd;
     
        protected BaseWidgetLayout Layout { get; private set; }

        public void Initialize(BaseWidgetLayout layout)
        {
            Layout = layout;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public void Play(WidgetAnimationType widgetAnimationType, bool instant, Action callback = null)
        {
            OnAnimationEnd += callback;

            switch (widgetAnimationType)
            {
                case WidgetAnimationType.Open:
                    PlayOpen(instant, OnAnimationEnded);
                    break;
                case WidgetAnimationType.Close:
                    PlayClose(instant, OnAnimationEnded);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(widgetAnimationType), widgetAnimationType, null);
            }
        }

        protected abstract void PlayOpen(bool instant, Action callback);

        protected abstract void PlayClose(bool instant, Action callback);
        
        private void OnAnimationEnded()
        {
            OnAnimationEnd?.Invoke();
            OnAnimationEnd = null;
        }
    }

    public enum WidgetAnimationType
    {
        Open,
        Close
    }
}