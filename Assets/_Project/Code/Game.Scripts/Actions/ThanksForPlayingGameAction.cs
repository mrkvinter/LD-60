using System;
using System.Collections.Generic;
using Code.Game.Core.ExecutorSystem.Actions;
using Game.Core;
using Game.Core.Contexts;
using Game.Scripts.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Actions
{
    [Serializable]
    public class ThanksForPlayingGameAction : GameAction
    {
        [Tooltip("Restart behind the black screen. Otherwise the player goes to the point and the game goes on.")]
        [SerializeField] private bool restart;

        [Tooltip("Id of a TeleportPoint in the scene.")]
        [HideIf(nameof(restart))]
        [ValueDropdown(nameof(PointIds))]
        [SerializeField] private string point;

        public override void Execute()
        {
            var screen = Object.FindAnyObjectByType<ThanksScreen>();
            if (restart)
            {
                screen.Show(RootServiceLocator.Resolve<IGameDirector>().RestartGame, false);
                return;
            }

            // The executor is shared and the next run overwrites its fields.
            var target = point;
            screen.Show(() => TeleportPlayerGameAction.Teleport(target), true);
        }

        private static IEnumerable<string> PointIds() => TeleportPlayerGameAction.PointIds();
    }
}
