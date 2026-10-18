using System;
using Code.Game.Core.ExecutorSystem.Actions;
using Game.Core;
using Game.Core.Contexts;

namespace Game.Scripts.Actions
{
    /// <summary>
    /// Restarts the game through the director, the same way the restart cheat does: behind the loading screen, from the
    /// main save if there is one, otherwise from the start.
    /// </summary>
    [Serializable]
    public class RestartGameAction : GameAction
    {
        public override void Execute() => RootServiceLocator.Resolve<IGameDirector>().RestartGame();
    }
}
