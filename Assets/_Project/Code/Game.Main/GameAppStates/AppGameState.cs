using System;
using System.Collections.Generic;
using Code.Cursor;
using Code.Game.Core;
using Code.Game.Core.ExecutorSystem;
using Code.Game.Scripts.EntitySystem;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Contexts;
using Game.Main.installers;
using Game.Scripts.Analytics;
using Game.Scripts.Construction;
using Game.Scripts.Items;
using Game.Scripts.UI;
using Game.Scripts.Upgrades;
using RG.DefinitionSystem.Core;
using Game.Scripts.SaveLoadSystem;
using Game.Utilities.Extensions;
using Game.UI.Base;
using KvinterGames;
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
        private AudioService.LoopSound rumble;

        private readonly HashSet<IPersistent> persistents = new();
        private GameSaveData loaded;

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
                builder.Register<SortingProgress>(Lifetime.Singleton);
                builder.Register<ResourceStorage>(Lifetime.Singleton);
                builder.Register<ConstructionProgress>(Lifetime.Singleton).AsSelf().As<IPostLoad>();
                builder.Register<AnalyticsTracker>(Lifetime.Singleton);
                builder.Register(_ => new UpgradeService(DefManager.GetSingletonDef<UpgradeSettingsDef>()), Lifetime.Singleton)
                    .AsSelf().As<IPersistent>();
                builder.RegisterEntryPoint<AutoSaveService>().AsSelf();
                builder.RegisterEntryPoint<UpgradeHud>().AsSelf();

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

            foreach (var registryObject in objectRegistry.Enumerate())
            {
                if (registryObject is IPersistent persistent) persistents.Add(persistent);
            }

            persistents.UnionWith(LifetimeScope.Container.ResolveAll<IPersistent>());

            objectRegistry.OnRegistered += InitMonoRegistry;
            objectRegistry.OnUnregistered += DeinitMonoRegistry;

            LifetimeScope.Container.Resolve<ActionExecutorSystem>();
            LifetimeScope.Container.Resolve<CursorService>();

            rumble = LifetimeScope.Container.Resolve<AudioService>().PlayLoop("Rumble");

            var hudSettings = DefManager.GetSingletonDef<HudSettingsDef>();
            UnityEngine.Object.Instantiate(hudSettings.ThanksScreen);
            UnityEngine.Object.Instantiate(hudSettings.PauseMenu).GetComponent<PauseMenu>().SetAmbience(rumble.AudioSource);

            await UniTask.DelayFrame(1);

            if (saveData != null)
            {
                Load(saveData);
            }

            // After the load, so restoring racks and buildings shows no points popups and sends no analytics.
            LifetimeScope.Container.Resolve<UpgradeHud>().Start();
            LifetimeScope.Container.Resolve<AnalyticsTracker>().Start();
            LifetimeScope.Container.Resolve<AutoSaveService>().Start();
        }

        public async UniTask Exit()
        {
            rumble?.Stop();
            rumble = null;

            if (objectRegistry != null)
            {
                objectRegistry.OnRegistered -= InitMonoRegistry;
                objectRegistry.OnUnregistered -= DeinitMonoRegistry;
            }

            persistents.Clear();
            loaded = null;

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
            foreach (var persistent in persistents)
            {
                persistent.Save(gameSaveData);
            }

            return gameSaveData;
        }

        // An object that turns up after the load, e.g. a rack inside a building the save had built, loads on arrival.
        private void InitMonoRegistry(ObjectRegistry.MonoRegistered monoRegistered)
        {
            if (monoRegistered is IPersistent persistent) persistents.Add(persistent);

            UniTask.DelayFrame(1).ContinueWith(() =>
            {
                monoRegistered.Initialize();
                monoRegistered.PostInitialize();

                if (loaded == null || monoRegistered is not IPersistent arrived) return;

                arrived.Load(loaded);
                (arrived as IPostLoad)?.PostLoad();
            }).Forget();
        }

        private void DeinitMonoRegistry(ObjectRegistry.MonoRegistered monoRegistered)
        {
            if (monoRegistered is IPersistent persistent) persistents.Remove(persistent);
        }

        private void Load(GameSaveData gameSaveData)
        {
            loaded = gameSaveData;

            foreach (var persistent in persistents)
            {
                persistent.Load(gameSaveData);
            }

            foreach (var persistent in persistents)
            {
                (persistent as IPostLoad)?.PostLoad();
            }

            foreach (var postLoad in LifetimeScope.Container.ResolveAll<IPostLoad>())
            {
                postLoad.PostLoad();
            }
        }
    }
}
