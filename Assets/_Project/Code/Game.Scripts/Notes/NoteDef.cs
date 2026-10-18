using System;
using Code.Game.Core.ExecutorSystem.Actions;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Notes
{
    [Serializable]
    public sealed class NoteDef : Definition
    {
        public string Title;
        [TextArea(5, 20)] public string Text;

        [Tooltip("Text on the close button. Empty: the button keeps its own text, OK.")]
        public string ButtonText;

        [Tooltip("Run after the close button closes the note.")]
        public GameActionJunction ButtonActions;
    }
}
