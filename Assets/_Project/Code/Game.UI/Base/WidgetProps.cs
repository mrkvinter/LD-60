using System;
using RG.DefinitionSystem.Core;
using RG.DefinitionSystem.Core.Constants;
using UnityEngine;

namespace Game.UI.Base
{
    [WithConstants]
    [Serializable]
    public class WidgetProps : Definition
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public CanvasTarget Target { get; private set; }
    }
}