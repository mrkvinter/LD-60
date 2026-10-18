using System;
using System.Collections.Generic;
using Code.Game.Core.ExecutorSystem.Actions;
using Game.Scripts.Movement;
using Game.Scripts.Player;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Actions
{

    [Serializable]
    public class TeleportPlayerGameAction : GameAction
    {
        [Tooltip("Id of a TeleportPoint in the scene.")]
        [ValueDropdown(nameof(PointIds))]
        [SerializeField] private string point;

        public override void Execute()
        {
            var target = TeleportPoint.Find(point);
            if (target == null)
            {
                Debug.LogError($"{nameof(TeleportPlayerGameAction)}: no {nameof(TeleportPoint)} with id '{point}'.");
                return;
            }

            var player = Object.FindAnyObjectByType<PlayerAdapter>();
            var movement = player != null ? player.Entity?.GetComponent<IMovementComponent>() : null;
            movement?.Teleport(target.transform.position, target.transform.rotation);
        }

        private static IEnumerable<string> PointIds()
        {
            foreach (var teleportPoint in Object.FindObjectsByType<TeleportPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                yield return teleportPoint.Id;
            }
        }
    }
}
