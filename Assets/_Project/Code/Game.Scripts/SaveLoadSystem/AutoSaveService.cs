using System;
using Game.Core;
using Game.Scripts.Player;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Game.Scripts.SaveLoadSystem
{
    public sealed class AutoSaveService : ITickable, IDisposable
    {
        private const float Interval = 10f;

        private readonly IGameDirector director;
        private PlayerInputComponent input;
        private bool isStarted;
        private float next;

        public AutoSaveService(IGameDirector director)
        {
            this.director = director;
        }

        // Not before the load: a save made earlier would hold a level that has not loaded yet.
        public void Start()
        {
            if (isStarted) return;

            isStarted = true;
            next = Time.unscaledTime + Interval;
            Application.focusChanged += OnFocusChanged;
            Application.quitting += TrySave;
        }

        public void Tick()
        {
            if (!isStarted || Time.unscaledTime < next) return;

            next = Time.unscaledTime + Interval;
            TrySave();
        }

        public void Dispose()
        {
            Application.focusChanged -= OnFocusChanged;
            Application.quitting -= TrySave;
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus) TrySave();
        }

        // A held player is mid-note, mid-menu or behind the thanks screen: the state is between two steps.
        private void TrySave()
        {
            if (!FindInput() || input.IsBlocked) return;

            director.SaveGame();
        }

        private bool FindInput()
        {
            if (input != null) return true;

            var player = Object.FindAnyObjectByType<PlayerAdapter>();
            input = player != null ? player.Entity?.GetComponent<PlayerInputComponent>() : null;
            return input != null;
        }
    }
}
