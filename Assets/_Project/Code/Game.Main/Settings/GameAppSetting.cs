using System;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Main.Settings
{
    [Serializable]
    public class GameAppSetting : SingletonDefinition
    {
        public string GameScene;
    }
}