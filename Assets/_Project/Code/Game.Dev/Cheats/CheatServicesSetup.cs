using System;
using Game.Core;
using Game.Core.Contexts;
using Game.Scripts.SaveLoadSystem;
using UnityEngine;

namespace Code.Cheats
{
    public static class CheatServicesSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Setup()
        {
            CheatServices.Resolver = Resolve;
            CheatServices.RestartGame = () => RootServiceLocator.Resolve<IGameDirector>().RestartGame();
            CheatServices.SaveProvider = new DebugSaveLoadService();
        }

        private static object Resolve(Type type)
        {
#if UNITY_EDITOR
            return Application.isPlaying ? GameServiceLocator.Resolve(type) : EditorServiceLocator.Resolve(type);
#else
            return GameServiceLocator.Resolve(type);
#endif
        }
    }
}
