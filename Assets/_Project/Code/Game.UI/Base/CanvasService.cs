using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Base
{
    public class CanvasService
    {
        private readonly Dictionary<CanvasTarget, CanvasData> canvases = new();

        public void RegisterCanvas(CanvasTarget target, Canvas canvas)
        {
            canvases[target] = new CanvasData(canvas);
        }

        public Canvas GetCanvas(CanvasTarget target)
        {
            return canvases[target].Canvas;
        }
        
        public void SetRaycasterActive(CanvasTarget target, bool value)
        {
            canvases[target].Raycaster.enabled = value;
        }
        
        private class CanvasData
        {
            public Canvas Canvas { get; }
            public GraphicRaycaster Raycaster { get; }
            
            public CanvasData(Canvas canvas)
            {
                Canvas = canvas;
                Raycaster = canvas.GetComponent<GraphicRaycaster>();
            }
        }
    }
}