using System;
using Code.Game.Core.ExecutorSystem.Actions;
using Game.Scripts.Portal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Actions
{
    /// <summary>Opens every <see cref="PortalLauncher"/> in the scene, as the machine comes back to life.</summary>
    [Serializable]
    public class LaunchPortalGameAction : GameAction
    {
        public override void Execute()
        {
            var launchers = Object.FindObjectsByType<PortalLauncher>(FindObjectsInactive.Exclude);
            if (launchers.Length == 0)
            {
                Debug.LogError($"{nameof(LaunchPortalGameAction)}: no {nameof(PortalLauncher)} in the scene.");
                return;
            }

            foreach (var launcher in launchers)
            {
                launcher.Launch();
            }
        }
    }
}
