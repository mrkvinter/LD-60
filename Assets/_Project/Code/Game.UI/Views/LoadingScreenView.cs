using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.UI.Views
{
    public class LoadingScreenView : MonoBehaviour
    {
        private const int DotsCount = 4;
        private const float DotsInterval = 0.5f;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text loadingText;

        private bool isShowed;
        private string originalText;
        private string[] textWithDots;

        protected void Awake()
        {
            originalText = loadingText.text;
            textWithDots = new string[DotsCount];
            for (var i = 0; i < textWithDots.Length; i++)
            {
                textWithDots[i] = originalText + new string('.', i);
            }
        }

        public void Show(Action callback)
        {
            isShowed = true;
            canvasGroup.DOKill();
            canvasGroup.DOFade(1, 0.5f).OnComplete(() => callback?.Invoke());
        }

        public void ShowImmediate()
        {
            isShowed = true;
            canvasGroup.alpha = 1;
        }

        public void Hide()
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0, 0.5f);
            isShowed = false;
        }

        public UniTask HideAsync()
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0, 0.5f);
            isShowed = false;

            return UniTask.WaitForSeconds(0.5f);
        }

        protected void Update()
        {
            if (!isShowed) return;

            loadingText.text = textWithDots[(int)(Time.time / DotsInterval) % DotsCount];
        }
    }
}
