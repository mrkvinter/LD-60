using UnityEngine;

namespace Game.Main
{
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // Initialize the game director
            GameStartupHandler.OnGameStart();
        }
    }
}