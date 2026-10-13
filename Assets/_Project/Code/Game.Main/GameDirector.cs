using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Main.GameAppStates;
using Game.UI.Base;
using Game.UI.Extensions;
using Game.UI.Views;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace Game.Main
{
   public sealed class GameDirector : IGameDirector
    {
        public const string MainSaveName = "MainSave";

        private readonly LoadingScreenView loadingScreen;

        private AppRootState appRootState;

        public GameDirector()
        {
            UniTaskScheduler.UnobservedTaskException += Debug.LogException;

            loadingScreen = GetLoadingScreen();
        }

        public void InitializeGame() => UniTask.Create(async () =>
        {
            loadingScreen.ShowImmediate();
            appRootState = new AppRootState(this, loadingScreen);

            await appRootState.Enter();

            var saveData = appRootState.SaveLoadService.HasSave(MainSaveName) ? appRootState.SaveLoadService.Load(MainSaveName) : null;
            await appRootState.AppGameState.Enter(saveData);

            //for nice loading screen effect
            await UniTask.Delay(100);

            await PlayIntroAndReveal();
        }).Forget();

        // The studio splash belongs to the first launch only — a restart drops straight
        // back into the kitchen.
        public void RestartGame() => UniTask.Create(async () =>
        {
            await ShowLoadingScreen();
            await appRootState.AppGameState.Exit();
            var saveData = appRootState.SaveLoadService.HasSave(MainSaveName) ? appRootState.SaveLoadService.Load(MainSaveName) : null;
            await appRootState.AppGameState.Enter(saveData);
            loadingScreen.Hide();
        }).Forget();

        private UniTask PlayIntroAndReveal()
        {
            loadingScreen.Hide();
            return UniTask.CompletedTask;
        }

        public void LoadLastSave() => UniTask.Create(async () =>
        {
            if (!appRootState.SaveLoadService.HasSave(MainSaveName))
            {
                Debug.LogError($"[ {nameof(GameDirector)} ]: No save data found");
                return;
            }

            await ShowLoadingScreen();
            await appRootState.AppGameState.Exit();
            var saveData = appRootState.SaveLoadService.Load(MainSaveName);
            await appRootState.AppGameState.Enter(saveData);
            loadingScreen.Hide();
        });

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SaveGame()
        {
            var saveData = appRootState.AppGameState.Save();
            appRootState.SaveLoadService.Save(MainSaveName, saveData);
        }

        private async UniTask ShowLoadingScreen()
        {
            var utcs = new UniTaskCompletionSource();
            loadingScreen.Show(() => utcs.TrySetResult());
            await utcs.Task;
        }

        private LoadingScreenView GetLoadingScreen()
        {
            var prefab = Resources.Load<GameObject>("LoadingScreen");
            var result = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(result);
            result.name = "[CANVAS] LoadingScreen";
            var canvas = result.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var loadingScreenView = result.GetComponentInChildren<LoadingScreenView>();

            return loadingScreenView;
        }
    }
}
