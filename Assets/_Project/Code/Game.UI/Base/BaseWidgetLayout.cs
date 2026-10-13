using Cysharp.Threading.Tasks;
using Game.UI.Animations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.UI.Base
{
    public abstract class BaseWidgetLayout : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public RectTransform RectTransform { get; private set; }
        [field: SerializeField, ReadOnly] public CanvasGroup CanvasGroup { get; private set; }
        [field: SerializeField] public BaseWidgetAnimation WidgetAnimation { get; private set; }

        private void FindRefs()
        {
            RectTransform = GetComponent<RectTransform>();
            CanvasGroup = GetComponent<CanvasGroup>();
            WidgetAnimation = GetComponent<BaseWidgetAnimation>();
        }

        [ContextMenu("Validate")]
        protected internal virtual void OnValidate()
        {
            FindRefs();
        }
        
#if UNITY_EDITOR
        [Button]
        private void TestOpen() => UniTask.Create(async () =>
        {
            if (WidgetAnimation == null) return;
           
            WidgetAnimation.Initialize(this);
            WidgetAnimation.Play(WidgetAnimationType.Close, true);
            var cts = new UniTaskCompletionSource();
            WidgetAnimation.Play(WidgetAnimationType.Open, false, () => cts.TrySetResult());
            await cts.Task;
        });
        
        [Button]
        private void TestClose() => UniTask.Create(async () =>
        {
            if (WidgetAnimation == null) return;
            
            WidgetAnimation.Initialize(this);
            WidgetAnimation.Play(WidgetAnimationType.Open, true);
            var cts = new UniTaskCompletionSource();
            WidgetAnimation.Play(WidgetAnimationType.Close, false, () => cts.TrySetResult());
            await cts.Task;
            await UniTask.Delay(1000);
            WidgetAnimation.Play(WidgetAnimationType.Open, true);
        });
#endif
    }
    
    public abstract class BasePopupLayout : BaseWidgetLayout
    {
        [field: SerializeField, ReadOnly] public CanvasGroup BackgroundCanvasGroup { get; private set; }


    }
}