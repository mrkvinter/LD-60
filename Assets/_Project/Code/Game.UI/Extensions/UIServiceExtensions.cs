using Cysharp.Threading.Tasks;
using Game.UI.Base;

namespace Game.UI.Extensions
{
    public static class UIServiceExtensions
    {
        public static async UniTask<TWindow> OpenAsync<TWindow>(this UIService uiService, IWidgetArgs args)  where TWindow : UIWidget 
        {
            var tcs = new UniTaskCompletionSource();
            var window = uiService.Show<TWindow>(args, () => tcs.TrySetResult());
            await tcs.Task;
            return window;
        }
        
        public static UniTask CloseAsync<TWindow>(this UIService uiService) where TWindow : UIWidget
        {
            var tcs = new UniTaskCompletionSource();
            uiService.Close<TWindow>(() => tcs.TrySetResult());
            return tcs.Task;
        }
    }
}