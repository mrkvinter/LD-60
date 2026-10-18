using System;
using RG.DefinitionSystem.Core;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI
{
    [Serializable]
    public sealed class HudSettingsDef : SingletonDefinition
    {
        public TMP_FontAsset PopupFont;
        public GameObject UpgradePanel;
    }
}
