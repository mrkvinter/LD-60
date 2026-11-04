using System;
using Code.Cursor;
using Code.Game.Core.ExecutorSystem;
using Code.Game.Core.ExecutorSystem.Actions;
using DG.Tweening;
using Game.Core.Contexts;
using Game.Scripts.Notes;
using Game.Scripts.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>
    /// Reads a picked-up note: the hand with the item list drops out of view and the hand with the note rises in its
    /// place. Like a menu, it frees the cursor and stops the player until its button closes it.
    /// </summary>
    public sealed class NoteView : MonoBehaviour
    {
        [Tooltip("The note hand that rises; its position in the scene is where it stands while open.")]
        [SerializeField] private RectTransform note;

        [Tooltip("The hand with the item list, dropped out of view while the note is open.")]
        [SerializeField] private RectTransform hand;

        [SerializeField] private TMP_Text text;
        [SerializeField] private Button closeButton;

        [Tooltip("Shown only for notes with a SecondButtonText.")]
        [SerializeField] private Button secondButton;

        [Tooltip("How far below its place a hand hides, in canvas pixels.")]
        [SerializeField] private float hideDistance = 800f;

        [SerializeField, Min(0f)] private float slideTime = 0.35f;

        private Vector2 noteShown;
        private Vector2 handShown;
        private TMP_Text closeLabel;
        private TMP_Text secondLabel;
        private string defaultCloseText;
        private PlayerInputComponent input;
        private CursorService cursor;
        private Sequence slide;
        private NoteDef shown;

        public bool IsOpen { get; private set; }

        public event Action<NoteDef> Shown;

        private void Awake()
        {
            noteShown = note.anchoredPosition;
            handShown = hand.anchoredPosition;
            note.anchoredPosition = noteShown + Vector2.down * hideDistance;

            closeLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null) defaultCloseText = closeLabel.text;
            secondLabel = secondButton.GetComponentInChildren<TMP_Text>(true);
            secondButton.gameObject.SetActive(false);

            closeButton.onClick.AddListener(OnCloseClicked);
            secondButton.onClick.AddListener(OnSecondClicked);
        }

        private void OnDestroy()
        {
            slide?.Kill();
            closeButton.onClick.RemoveListener(OnCloseClicked);
            secondButton.onClick.RemoveListener(OnSecondClicked);
        }

        /// <summary>Opens the note unless a note or a menu is open already.</summary>
        public bool TryShow(NoteDef def)
        {
            if (IsOpen || !FindInput() || input.IsBlocked) return false;

            text.text = string.IsNullOrEmpty(def.Title) ? def.Text : $"<size=130%><b>{def.Title}</b></size>\n\n{def.Text}";
            if (closeLabel != null)
            {
                closeLabel.text = string.IsNullOrWhiteSpace(def.ButtonText) ? defaultCloseText : def.ButtonText;
            }

            var hasSecondButton = !string.IsNullOrWhiteSpace(def.SecondButtonText);
            secondButton.gameObject.SetActive(hasSecondButton);
            if (hasSecondButton) secondLabel.text = def.SecondButtonText;

            shown = def;
            IsOpen = true;
            input.IsBlocked = true;
            cursor ??= GameServiceLocator.Resolve<CursorService>();
            cursor.ReleaseLock();

            Slide(handShown + Vector2.down * hideDistance, noteShown, Ease.OutCubic);
            Shown?.Invoke(def);
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;

            shown = null;
            IsOpen = false;
            input.IsBlocked = false;
            cursor.RequestLock();

            Slide(handShown, noteShown + Vector2.down * hideDistance, Ease.InOutCubic);
        }

        // The note's actions run once it is closed, so one that moves the player on (a restart, a teleport) finds the
        // controls given back.
        private void OnCloseClicked() => CloseAndRun(shown?.ButtonActions ?? default);

        private void OnSecondClicked() => CloseAndRun(shown?.SecondButtonActions ?? default);

        private void CloseAndRun(GameActionJunction actions)
        {
            Close();

            if (actions.Actions is { Length: > 0 })
            {
                GameServiceLocator.Resolve<ActionExecutorSystem>().Execute(actions);
            }
        }

        private void Slide(Vector2 handTarget, Vector2 noteTarget, Ease ease)
        {
            slide?.Kill();
            slide = DOTween.Sequence()
                .Join(DOTween.To(() => hand.anchoredPosition, value => hand.anchoredPosition = value, handTarget, slideTime).SetEase(ease))
                .Join(DOTween.To(() => note.anchoredPosition, value => note.anchoredPosition = value, noteTarget, slideTime).SetEase(ease));
        }

        private bool FindInput()
        {
            if (input != null) return true;

            var player = FindAnyObjectByType<PlayerAdapter>();
            input = player != null ? player.Entity?.GetComponent<PlayerInputComponent>() : null;
            return input != null;
        }
    }
}
