using System;
using Game.UI.Base;

namespace Game.UI.Animations
{
    public interface IWidgetAnimation
    {
        void Initialize(BaseWidgetLayout layout);
        void Play(WidgetAnimationType widgetAnimationType, bool instant, Action callback = null);
    }
}