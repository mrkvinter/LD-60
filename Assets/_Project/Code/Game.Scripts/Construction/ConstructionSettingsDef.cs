using System;
using RG.DefinitionSystem.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Scripts.Construction
{
    [Serializable]
    public sealed class ConstructionSettingsDef : SingletonDefinition
    {
        [Tooltip("Look of an unbuilt object.")]
        public Material GhostMaterial;

        [Tooltip("The label over a site shows while the camera is this close.")]
        [SuffixLabel("m")] public float LabelDistance = 6f;

        [Tooltip("Gap between the top of the site and its label.")]
        [SuffixLabel("m")] public float LabelHeight = 0.3f;

        public float LabelFontSize = 2f;
        public Color DoneColor = new(0.55f, 1f, 0.55f, 1f);
    }
}
