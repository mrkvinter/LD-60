using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Upgrades
{
    public sealed class UpgradeHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text hoverText;
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private CanvasGroup pointsPopup;
        [SerializeField] private TMP_Text refuseText;
        [SerializeField] private CanvasGroup refusePopup;
        [SerializeField] private UpgradePanelView panel;

        [SerializeField, Min(0f)] private float fadeTime = 0.2f;
        [SerializeField, Min(0f)] private float pointsHoldTime = 2f;
        [SerializeField, Min(0f)] private float refuseHoldTime = 1.2f;

        private Sequence pointsFade;
        private Sequence refuseFade;
        private int pointsShown;

        public UpgradePanelView Panel => panel;
        public bool IsPanelOpen => panel.gameObject.activeSelf;

        private void Awake()
        {
            pointsPopup.alpha = 0f;
            refusePopup.alpha = 0f;
            panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            pointsFade?.Kill();
            refuseFade?.Kill();
        }

        public void SetHover(string text) => hoverText.text = text;

        public void SetPanelOpen(bool open) => panel.gameObject.SetActive(open);

        // Points that come while the popup is up add to it, so filling several racks at once reads as one sum.
        public void ShowPointsEarned(int points)
        {
            pointsShown += points;
            pointsText.text = $"+{pointsShown} {(pointsShown == 1 ? "POINT" : "POINTS")}\n<size=50%>[TAB] Upgrade</size>";

            pointsFade?.Kill();
            pointsText.rectTransform.localScale = Vector3.one;
            pointsFade = DOTween.Sequence()
                .Append(DOTween.To(() => pointsPopup.alpha, value => pointsPopup.alpha = value, 1f, fadeTime))
                .Join(pointsText.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.35f, 6))
                .AppendInterval(pointsHoldTime)
                .Append(DOTween.To(() => pointsPopup.alpha, value => pointsPopup.alpha = value, 0f, fadeTime * 2f))
                .OnComplete(() => pointsShown = 0);
        }

        public void ShowRefused(string text)
        {
            refuseText.text = text;

            refuseFade?.Kill();
            refuseText.rectTransform.localScale = Vector3.one;
            refuseFade = DOTween.Sequence()
                .Append(DOTween.To(() => refusePopup.alpha, value => refusePopup.alpha = value, 1f, fadeTime * 0.5f))
                .Join(refuseText.rectTransform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6))
                .AppendInterval(refuseHoldTime)
                .Append(DOTween.To(() => refusePopup.alpha, value => refusePopup.alpha = value, 0f, fadeTime * 2f));
        }
    }
}
