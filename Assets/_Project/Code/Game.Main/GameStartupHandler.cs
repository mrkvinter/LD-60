using UnityEngine;

namespace Game.Main
{
    public static class GameStartupHandler
    {
        private static GameDirector gameDirector;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void OnGameStart()
        {
            if (gameDirector != null)
            {
                Debug.LogWarning("GameDirector is already initialized.");
                return;
            }

            gameDirector = new GameDirector();
            gameDirector.InitializeGame();

            Application.quitting += OnGameEnd;
        }

        private static void OnGameEnd()
        {
            gameDirector = null;
            Application.quitting -= OnGameEnd;
        }
    }
}