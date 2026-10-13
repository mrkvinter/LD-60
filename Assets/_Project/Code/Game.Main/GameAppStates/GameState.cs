using Code.Game.Core;
using Cysharp.Threading.Tasks;
using Game.Main.Settings;
using Game.Scripts.SaveLoadSystem;
using Game.UI.Base;
using Game.Utilities;
using RG.DefinitionSystem.Core;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Game.Main.GameAppStates
{
    public class GameState
    {
        private readonly LifetimeScope parentLifetimeScope;
        private readonly GameAppSetting appGameSettings;

        private Scene mainScene;

        public LifetimeScope LifetimeScope { get; private set; }

        public GameState(LifetimeScope parentLifetimeScope)
        {
            this.parentLifetimeScope = parentLifetimeScope;

            appGameSettings = DefManager.GetSingletonDef<GameAppSetting>();
        }

        public async UniTask Enter(GameSaveData saveData = null)
        {
            var sceneMain = appGameSettings.GameScene;
            mainScene = SceneManager.GetSceneByName(sceneMain);
            if (!mainScene.IsValid() || !mainScene.isLoaded)
            {
                await SceneManager.LoadSceneAsync(sceneMain, LoadSceneMode.Additive).ToUniTask();
                mainScene = SceneManager.GetSceneByName(sceneMain);
            }

            SceneManager.SetActiveScene(mainScene);
            var canvasService = parentLifetimeScope.Container.Resolve<CanvasService>();
            var sceneLinks = SceneObjectsUtilities.RecursiveFindObject<SceneLinks>(mainScene.GetRootGameObjects());
            if (sceneLinks != null)
            {
                canvasService.RegisterCanvas(CanvasTarget.GameUI, sceneLinks.CanvasGame);

                // Main-target widgets (intro splash, black screen) go onto the scene's camera
                // canvas so the camera's post-processing volume affects them too.
                if (sceneLinks.CanvasMain != null)
                {
                    canvasService.RegisterCanvas(CanvasTarget.Main, sceneLinks.CanvasMain);
                }
            }

            LifetimeScope = parentLifetimeScope.CreateChild();

            await UniTask.DelayFrame(2);
        }

        public async UniTask Exit()
        {
            await SceneManager.UnloadSceneAsync(mainScene).ToUniTask();
        }
    }
}