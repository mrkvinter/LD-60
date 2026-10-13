using System;
using UnityEngine;

namespace Code.Cursor
{
    public class CursorService : IDisposable
    {
        private int lockRequests;

        public CursorService()
        {
            Application.focusChanged += OnFocusChanged;
            Apply();
        }

        public void Dispose()
        {
            Application.focusChanged -= OnFocusChanged;
        }

        public void RequestLock()
        {
            lockRequests++;
            Apply();
        }

        public void ReleaseLock()
        {
            if (lockRequests == 0) return;

            lockRequests--;
            Apply();
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (hasFocus) Apply();
        }

        private void Apply()
        {
            var locked = lockRequests > 0;
            UnityEngine.Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !locked;
        }
    }
}
