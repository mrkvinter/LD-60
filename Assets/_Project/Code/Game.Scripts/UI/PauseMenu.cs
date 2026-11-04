#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using Code.Cursor;
using Code.Prefs;
using Game.Core;
using Game.Core.Contexts;
using Game.Scripts.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider ambienceSlider;

        private readonly FloatPlayerPref volume = new($"{nameof(PauseMenu)}.Volume", 1f);
        private readonly FloatPlayerPref ambienceVolume = new($"{nameof(PauseMenu)}.Ambience", 1f);

        private InputAction escape;
        private CursorService cursor;
        private PlayerInputComponent input;
        private AudioSource ambience;

        private bool IsOpen => panel.activeSelf;

#if UNITY_WEBGL && !UNITY_EDITOR
        private bool wasPointerLocked;

        [DllImport("__Internal")]
        private static extern bool IsPointerLocked();

        // The browser takes Esc for itself to drop the pointer lock, so the key may never reach the game.
        private void Update()
        {
            var isPointerLocked = IsPointerLocked();
            if (wasPointerLocked && !isPointerLocked) Open();
            wasPointerLocked = isPointerLocked;
        }
#endif

        private void Awake()
        {
            cursor = GameServiceLocator.Resolve<CursorService>();
            panel.SetActive(false);

            AudioListener.volume = volume.Value;
            volumeSlider.SetValueWithoutNotify(volume.Value);
            ambienceSlider.SetValueWithoutNotify(ambienceVolume.Value);

            continueButton.onClick.AddListener(Close);
            restartButton.onClick.AddListener(Restart);
            volumeSlider.onValueChanged.AddListener(SetVolume);
            ambienceSlider.onValueChanged.AddListener(SetAmbienceVolume);

            escape = new InputAction(binding: "<Keyboard>/escape");
            escape.performed += OnEscape;
            escape.Enable();
        }

        private void OnDestroy()
        {
            escape.Dispose();
            Time.timeScale = 1f;
        }

        public void SetAmbience(AudioSource source)
        {
            ambience = source;
            ambience.volume = ambienceVolume.Value;
        }

        private void OnEscape(InputAction.CallbackContext context)
        {
            if (IsOpen) Close();
            else Open();
        }

        // Another window (a note, the upgrades) or the thanks screen holding the player keeps the menu shut.
        private void Open()
        {
            if (IsOpen || !FindInput() || input.IsBlocked) return;

            RootServiceLocator.Resolve<IGameDirector>().SaveGame();

            panel.SetActive(true);
            input.IsBlocked = true;
            cursor.ReleaseLock();
            Time.timeScale = 0f;
        }

        private void Close()
        {
            panel.SetActive(false);
            input.IsBlocked = false;
            cursor.RequestLock();
            Time.timeScale = 1f;

            EventSystem.current.SetSelectedGameObject(null);
            PlayerPrefs.Save();
        }

        // The player stays held, so the menu cannot reopen while the game tears down.
        private void Restart()
        {
            panel.SetActive(false);
            Time.timeScale = 1f;

            PlayerPrefs.Save();
            RootServiceLocator.Resolve<IGameDirector>().RestartGame();
        }

        private void SetVolume(float value)
        {
            volume.Value = value;
            AudioListener.volume = value;
        }

        private void SetAmbienceVolume(float value)
        {
            ambienceVolume.Value = value;
            ambience.volume = value;
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
