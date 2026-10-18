using DG.Tweening;
using Game.Scripts.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>
    /// When the player unlocks an ability: the camera's power-up volume flares up and settles back, and a note in the
    /// middle of the screen says what was unlocked and how to use it.
    /// </summary>
    public sealed class AbilityUnlockFeedback : MonoBehaviour
    {
        [SerializeField] private Volume volume;

        [Header("Flare")]
        [SerializeField, Min(0f)] private float riseTime = 0.25f;
        [SerializeField, Min(0f)] private float fallTime = 0.75f;

        [Header("Popup")]
        [SerializeField, Min(0f)] private float popupFadeTime = 0.25f;
        [SerializeField, Min(0f)] private float popupHoldTime = 2.5f;

        private PlayerAbilitiesComponent abilities;
        private CanvasGroup popup;
        private TMP_Text popupText;
        private Sequence flare;
        private Sequence popupFade;

        private void Reset()
        {
            volume = GetComponent<Volume>();
        }

        private void Awake()
        {
            if (volume == null) volume = GetComponent<Volume>();
            if (volume != null) volume.weight = 0f;

            BuildPopup();
        }

        // The player entity appears once the game state has initialized the scene, after this object wakes.
        private void Update()
        {
            if (abilities != null) return;

            var player = FindAnyObjectByType<PlayerAdapter>();
            abilities = player != null ? player.Entity?.GetComponent<PlayerAbilitiesComponent>() : null;
            if (abilities != null) abilities.Unlocked += OnUnlocked;
        }

        private void OnDestroy()
        {
            if (abilities != null) abilities.Unlocked -= OnUnlocked;
            flare?.Kill();
            popupFade?.Kill();
            if (popup != null) Destroy(popup.gameObject);
        }

        private void OnUnlocked(PlayerAbility ability)
        {
            Flare();
            ShowPopup(ability);
        }

        private void Flare()
        {
            if (volume == null) return;

            flare?.Kill();
            flare = DOTween.Sequence()
                .Append(DOTween.To(() => volume.weight, value => volume.weight = value, 1f, riseTime).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => volume.weight, value => volume.weight = value, 0f, fallTime).SetEase(Ease.InQuad));
        }

        private void ShowPopup(PlayerAbility ability)
        {
            var (title, hint) = ability switch
            {
                PlayerAbility.Sprint => ("Sprint", "Hold Shift to run"),
                PlayerAbility.Jump => ("Jump", "Press Space"),
                PlayerAbility.DoubleJump => ("Double jump", "Press Space again in the air"),
                _ => (ability.ToString(), string.Empty)
            };

            popupText.text = $"<size=60%>ABILITY UNLOCKED</size>\n{title}\n<size=55%>{hint}</size>";

            popupFade?.Kill();
            popupFade = DOTween.Sequence()
                .Append(DOTween.To(() => popup.alpha, value => popup.alpha = value, 1f, popupFadeTime))
                .AppendInterval(popupHoldTime)
                .Append(DOTween.To(() => popup.alpha, value => popup.alpha = value, 0f, popupFadeTime * 2f));
        }

        // Its own overlay canvas, so the popup does not depend on what the scene canvases hold.
        private void BuildPopup()
        {
            var root = new GameObject("Ability Unlock Popup", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            popup = root.AddComponent<CanvasGroup>();
            popup.alpha = 0f;
            popup.interactable = false;
            popup.blocksRaycasts = false;

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            popupText = textObject.AddComponent<TextMeshProUGUI>();
            popupText.fontSize = 72f;
            popupText.alignment = TextAlignmentOptions.Center;
            popupText.textWrappingMode = TextWrappingModes.NoWrap;
            popupText.raycastTarget = false;

            var rect = popupText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.7f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1400f, 300f);
        }
    }
}
