using System;
using Game.UI.Animations;
using UnityEngine;

namespace Game.UI.Base
{
    public abstract class UIWidget
    {
        internal event Action<UIWidget> CloseRequested;

        public abstract string Id { get; }
        public abstract void Show(IWidgetArgs args, Action callback = null);
        public abstract void Hide(Action callback = null);
        public abstract void AssignLayout(BaseWidgetLayout layout);

        protected void Close() => CloseRequested?.Invoke(this);
    }

    internal interface IWithLayout
    {
        BaseWidgetLayout WidgetLayout { get; }
    }

    /// <summary>
    /// Base class for all UI widgets.
    /// Lifecycle of widget:
    /// 1. OnLayoutAssigned - called when widget is created and layout is assigned to widget.
    ///     Use this method to initialize layout components.
    /// 2. OnShow - called when widget is requested to be shown. It starts opening animation.
    ///     Use this method to initialize widget with arguments passed to it.
    /// 3. OnAppear - called when opening animation completes.
    ///     Use this method to initialize widget after it is completely shown.
    /// 4. OnClose - called when widget is requested to be closed. It starts closing animation.
    ///     Use this method to stop any ongoing processes or clean up widget before it is closed.
    /// 5. OnDisappear - called when closing animation completes.
    ///     Use this method to clean up widget after it is completely closed.
    /// </summary>
    /// <typeparam name="TLayout"></typeparam>
    public abstract class UIWidget<TLayout> : UIWidget, IWithLayout where TLayout : BaseWidgetLayout
    {
        public BaseWidgetLayout WidgetLayout => Layout;

        protected TLayout Layout { get; private set; }

        private IWidgetAnimation animation;

        public override void AssignLayout(BaseWidgetLayout layout)
        {
            Layout = (TLayout)layout;
            animation = Layout.WidgetAnimation;
            animation ??= new NoAnimation();
            animation.Initialize(Layout);

            OnLayoutAssigned();

            animation.Play(WidgetAnimationType.Close, true);
        }

        public sealed override void Show(IWidgetArgs args, Action callback = null)
        {
            Layout.gameObject.SetActive(true);

            OnShow(args);
            animation.Play(WidgetAnimationType.Open, args.IsInstantShow, Callback);
            return;

            void Callback()
            {
                callback?.Invoke();
                OnAppear();
            }
        }

        public sealed override void Hide(Action callback = null)
        {
            OnClose();
            animation.Play(WidgetAnimationType.Close, false, Callback);
            return;

            void Callback()
            {
                callback?.Invoke();
                OnDisappear();
                Layout.gameObject.SetActive(false);
            }
        }

        protected virtual void OnLayoutAssigned()
        {
        }

        /// <summary>
        /// Called when widget opening animation starts.
        /// </summary>
        /// <param name="args">Arguments passed to widget.</param>
        protected virtual void OnShow(IWidgetArgs args)
        {
        }

        /// <summary>
        /// Called when widget opening animation completes.
        /// </summary>
        protected virtual void OnAppear()
        {
        }

        /// <summary>
        /// Called when widget is requested to be closed.
        /// </summary>
        protected virtual void OnClose()
        {
        }

        /// <summary>
        /// Called when widget closing animation completes.
        /// </summary>
        protected virtual void OnDisappear()
        {
        }
    }

    public abstract class UIWidget<TLayout, TArgs> : UIWidget<TLayout>
        where TLayout : BaseWidgetLayout where TArgs : class, IWidgetArgs
    {
        protected TArgs Arguments { get; private set; }

        protected sealed override void OnShow(IWidgetArgs args)
        {
            var nativeArgs = args as TArgs;
            if (nativeArgs == null)
            {
                Debug.LogWarning($"Passed invalid args to widget of type: [{GetType()}]");
                return;
            }
            
            Arguments = nativeArgs;
            OnShow(nativeArgs);
        }

        protected virtual void OnShow(TArgs args)
        {
        }
    }
}