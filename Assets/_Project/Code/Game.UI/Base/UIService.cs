using System;
using System.Collections.Generic;
using Game.Core;
using RG.DefinitionSystem.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.UI.Base
{
    public class UIService
    {
        private readonly CanvasService canvasService;
        private readonly Dictionary<Type, UIWidget> widgetsByType = new();
        private readonly Dictionary<string, UIWidget> widgetsById = new();
        private readonly List<UIWidget> activeWidgets = new();

        public UIService(CanvasService canvasService)
        {
            this.canvasService = canvasService;
        }

        public TWindow Show<TWindow>(IWidgetArgs args, Action callback = null) where TWindow : UIWidget
        {
            var typeWindow = typeof(TWindow);
            if (!widgetsByType.TryGetValue(typeWindow, out var window))
            {
                window = CreateWindow(typeWindow);
            }

            window.CloseRequested += OnCloseRequested;
            window.Show(args, callback);
            activeWidgets.Add(window);
            
            return (TWindow)window;
        }

        public TWindow Show<TWindow>(TWindow window, IWidgetArgs args, Action callback = null) where TWindow : UIWidget
        {
            var typeWindow = typeof(TWindow);
            if (!widgetsByType.TryGetValue(typeWindow, out var win) || win != window)
            {
                throw new Exception($"Window of type {typeWindow} is not registered");
            }
            
            window.CloseRequested += OnCloseRequested;
            window.Show(args, callback);
            activeWidgets.Add(window);
            
            return window;
        }

        public TWidget Create<TWidget>() where TWidget : UIWidget
        {
            var typeWidget = typeof(TWidget);
            var widget = (TWidget)CreateWindow(typeWidget);

            return widget;
        }

        public TWidget GetOrCreate<TWidget>() where TWidget : UIWidget
        {
            var typeWidget = typeof(TWidget);
            if (!widgetsByType.TryGetValue(typeWidget, out var widget))
            {
                widget = Create<TWidget>();
                widgetsByType[typeWidget] = widget;
            }

            return (TWidget) widget;
        }

        public void Close<TWindow>(Action onClose = null)
        {
            var typeWindow = typeof(TWindow);
            if (widgetsByType.TryGetValue(typeWindow, out var window))
            {
                Close(window.Id, onClose);
            }
        }

        public void Close(string id, Action onClose = null)
        {
            if (widgetsById.TryGetValue(id, out var window))
            {
                if (activeWidgets.Remove(window))
                {
                    window.CloseRequested -= OnCloseRequested;
                    window.Hide(onClose);
                }
            }
        }
        
        public void Close(UIWidget window)
        {
            Close(window.Id);
        }

        public bool IsShown<TWindow>()
        {
            var typeWindow = typeof(TWindow);
            return widgetsByType.TryGetValue(typeWindow, out var window) && activeWidgets.Contains(window);
        }

        public void Clear()
        {
            foreach (var window in activeWidgets)
            {
                window.CloseRequested -= OnCloseRequested;
            }

            activeWidgets.Clear();
            widgetsByType.Clear();
            widgetsById.Clear();
        }

        private void OnCloseRequested(UIWidget window)
        {
            Close(window.Id);
        }
        
        private UIWidget CreateWindow(Type typeWindow)
        {
            var window = Activator.CreateInstance(typeWindow) as UIWidget;
            if (window == null)
            {
                throw new Exception($"Failed to create window of type {typeWindow}");
            }

            widgetsByType.Add(typeWindow, window);
            widgetsById.Add(window.Id, window);
            var props = DefManager.GetDef<WidgetProps>(window.Id);
            var layout = Object.Instantiate(props.Prefab).GetComponent<BaseWidgetLayout>();
            layout.gameObject.SetActive(false);
            if (props.Target != CanvasTarget.None)
            {
                var canvas = canvasService.GetCanvas(props.Target);
                layout.RectTransform.SetParent(canvas.transform, false);
            }

            layout.RectTransform.SetAsLastSibling();

            window.AssignLayout(layout);

            return window;
        }

        public T Get<T>() where T : UIWidget
        {
            var type = typeof(T);
            if (widgetsByType.TryGetValue(type, out var widget))
            {
                return (T)widget;
            }

            return null;
        }
    }

    public enum CanvasTarget
    {
        None,
        Main,
        GameUI
    }
}