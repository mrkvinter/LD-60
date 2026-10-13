using System;
using Game.UI.Base;

namespace Game.UI.Animations
{
    public class NoAnimation : IWidgetAnimation
    {
        private BaseWidgetLayout layout;

        public void Initialize(BaseWidgetLayout layout)
        {
            this.layout = layout;
        }

        public void Play(WidgetAnimationType widgetAnimationType, bool instant, Action callback = null)
        {
            layout.gameObject.SetActive(widgetAnimationType == WidgetAnimationType.Open);
            callback?.Invoke();
        }
    }
}