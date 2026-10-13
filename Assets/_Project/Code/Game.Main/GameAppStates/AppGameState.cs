using System;
using Code.Cursor;
using Code.Game.Core;
using Code.Game.Core.ExecutorSystem;
using Code.Game.Scripts.EntitySystem;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Contexts;
using Game.Main.installers;
using Game.Scripts.SaveLoadSystem;
using Game.Utilities.Extensions;
using Game.UI.Base;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Game.Main.GameAppStates
{
    public class AppGameState
    {
        private readonly LifetimeScope parentLifetimeScope;

        public LifetimeScope LifetimeScope { get; private set; }

        private GameState gameState;
        private ObjectRegistry objectRegistry;
        private Scene scene;

        public AppGameState(LifetimeScope parentLifetimeScope)
        {
            this.parentLifetimeScope = parentLifetimeScope;
        }

        public async UniTask Enter(GameSaveData saveData = null)
        {
            scene = SceneManager.CreateScene("AppGameScene");
            scene.name = "AppGameScene";
            SceneManager.SetActiveScene(scene);

            LifetimeScope = parentLifetimeScope.CreateChild(builder =>
            {
                builder.RegisterBuildCallback(GameServiceLocator.Initialize);

                // TODO: register game services here
                builder.Register<GameContext>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<CursorService>(Lifetime.Singleton).AsSelf();
                builder.Register<EntityService>(Lifetime.Singleton)
                    .WithParameter<Action<Exception>>(Debug.LogException)
                    .AsImplementedInterfaces().AsSelf();

                ExecutorInstaller.Install(builder);
            });

            gameState = new GameState(LifetimeScope);

            await gameState.Enter(saveData);

            objectRegistry = LifetimeScope.Container.Resolve<ObjectRegistry>();
            foreach (var registryObject in objectRegistry.Enumerate())
            {
                try
                {
                    registryObject.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to initialize registry object.", registryObject);
                    Debug.LogException(ex);
                }
            }

            foreach (var registryObject in objectRegistry.Enumerate())
            {
                try
                {
                    registryObject.PostInitialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to post-initialize registry object.", registryObject);
                    Debug.LogException(ex);
                }
            }

            objectRegistry.OnRegistered += InitMonoRegistry;

            LifetimeScope.Container.Resolve<ActionExecutorSystem>();
            LifetimeScope.Container.Resolve<CursorService>();

            await UniTask.DelayFrame(1);

            if (saveData != null)
            {
                Load(saveData);
            }
        }

        public async UniTask Exit()
        {
            if (objectRegistry != null)
            {
                objectRegistry.OnRegistered -= InitMonoRegistry;
            }

            if (gameState != null)
            {
                await gameState.Exit();
            }

            var uiService = LifetimeScope.Container.Resolve<UIService>();
            uiService.Clear();
            LifetimeScope.Dispose();
            GameServiceLocator.Clear();

            await SceneManager.UnloadSceneAsync(scene).ToUniTask();
        }

        public GameSaveData Save()
        {
            var gameSaveData = new GameSaveData();
            foreach (var persistent in LifetimeScope.Container.ResolveAll<IPersistent>())
            {
                persistent.Save(gameSaveData);
            }

            return gameSaveData;
        }

        private void InitMonoRegistry(ObjectRegistry.MonoRegistered monoRegistered)
        {
            UniTask.DelayFrame(1).ContinueWith(() =>
            {
                monoRegistered.Initialize();
                monoRegistered.PostInitialize();
            }).Forget();
        }

        private void Load(GameSaveData gameSaveData)
        {
            foreach (var persistent in LifetimeScope.Container.ResolveAll<IPersistent>())
            {
                persistent.Load(gameSaveData);
            }
        }
    }
}
