using System;
using Code.Game.Core;
using Code.Game.Main.Installers;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Contexts;
using Game.Main.installers;
using Game.Scripts.SaveLoadSystem;
using Game.UI.Base;
using Game.UI.Views;
using KvinterGames;
using RG.DefinitionSystem.UnityAdapter;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Game.Main.GameAppStates
{
    public class AppRootState
    {
        private const string MainCanvasResource = "CanvasMain";

        private readonly LoadingScreenView loadingScreen;
        private readonly IGameDirector gameDirector;

        private AppGameState _appGameState;
        private ISaveLoadService saveLoadService;
        
        public AppGameState AppGameState => _appGameState;
        public ISaveLoadService SaveLoadService => saveLoadService;
        public LifetimeScope RootLifetimeScope { get; private set; }

        public AppRootState(IGameDirector gameDirector, LoadingScreenView loadingScreenView)
        {
            this.gameDirector = gameDirector;
            loadingScreen = loadingScreenView;
        }
        
        public async UniTask Enter()
        {
            UniTaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
            var rootScene = SceneManager.CreateScene("RootScene");
            rootScene.name = "RootScene";
            SceneManager.MoveGameObjectToScene(loadingScreen.transform.root.gameObject, rootScene);
            SceneManager.SetActiveScene(rootScene);
            var objectRegistry = new ObjectRegistry();
            objectRegistry.Initialize();

            // GameAnalytics.Initialize();
            await CreateQuantumConsole();
            await CreateEventSystem();
            var audioService = await CreateSoundController();
            var mainCanvas = await CreateMainCanvas();
            var database = await Resources.LoadAsync<DefinitionDatabase>("DefinitionDatabase") as DefinitionDatabase;
            RootLifetimeScope = CreateRootLifetimeScope(database, audioService, objectRegistry);

            RootLifetimeScope.Container.Resolve<CanvasService>().RegisterCanvas(CanvasTarget.Main, mainCanvas);

            var saveStorage = new FileSaveStorage();
#if UNITY_EDITOR
            var debugSaves = new DebugSaveLoadService();
            debugSaves.ReplaceSaveIfNeeded(saveStorage, GameDirector.MainSaveName);
#endif
            saveLoadService = new SaveLoadService(new NewtonsoftJsonSaveSerializer(), saveStorage);
#if UNITY_EDITOR
            new SaveCaptureService(debugSaves).Attach(saveLoadService, saveStorage);
#endif
            
            _appGameState = new AppGameState(RootLifetimeScope);
        }

        private void HandleUnobservedTaskException(Exception obj)
        {
            Debug.LogError($"Unobserved exception: {obj}");
            // Handle the exception (e.g., log it, show a message to the user, etc.)
        }

        private LifetimeScope CreateRootLifetimeScope(DefinitionDatabase definitionDatabase, AudioService audioService, ObjectRegistry objectRegistry)
        {
            var lifetimeScope = LifetimeScope.Create(builder =>
            {
                builder.RegisterBuildCallback(RootServiceLocator.Initialize);
                
                builder.RegisterInstance(gameDirector).As<IGameDirector>();
                builder.RegisterInstance(audioService).As<AudioService>();
                builder.RegisterInstance(objectRegistry).As<ObjectRegistry>();
                builder.Register<CanvasService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                builder.Register<UIService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                // builder.Register<GameCheatFlags>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                DefInstaller.Install(builder, definitionDatabase);
                MessagePipeInstaller.Install(builder);
            });

            return lifetimeScope;
        }

        private static async UniTask<Canvas> CreateMainCanvas()
        {
            var canvasPrefab = await Resources.LoadAsync<GameObject>(MainCanvasResource).ToUniTask();
            var canvasObject = Object.Instantiate(canvasPrefab) as GameObject;

            return canvasObject.GetComponent<Canvas>();
        }

        private static async UniTask CreateEventSystem()
        {
            var eventSystemPrefab = await Resources.LoadAsync<GameObject>("EventSystem").ToUniTask();
            Object.Instantiate(eventSystemPrefab);
        }

        private static async UniTask<AudioService> CreateSoundController()
        {
            var soundControllerPrefab = await Resources.LoadAsync<GameObject>("Sounds").ToUniTask();
            var o = Object.Instantiate(soundControllerPrefab) as GameObject;
            
            var audioService = o.GetComponent<AudioService>();
            return audioService;
        }
        
        private static async UniTask CreateQuantumConsole()
        {
#if QC_ENABLED && (UNITY_EDITOR || DEVELOPMENT_BUILD)
            var quantumConsolePrefab = await Resources.LoadAsync<GameObject>("Quantum Console (SRP)").ToUniTask();
            Object.Instantiate(quantumConsolePrefab);
#endif
        }
    }
}