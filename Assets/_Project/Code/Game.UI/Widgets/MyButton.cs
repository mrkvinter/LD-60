using System;
using System.Linq;
using Code.Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.UI
{
    [RequireComponent(typeof(AdditionalGraphics))]
    public class MyButton : Button
    {
        private BaseAdditionalGraphics[] additionalGraphics;

        public event Action OnPointerEnterEvent;
        public event Action OnPointerExitEvent;

        protected override void Awake()
        {
            base.Awake();
            additionalGraphics = GetComponents<BaseAdditionalGraphics>();
        }

        public void SetBlinkObjectVisible(bool visible)
        {
            var blinkObject = additionalGraphics.FirstOrDefault(e => e is BlinkAdditionalGraphics) as BlinkAdditionalGraphics;
            if (blinkObject != null)
                blinkObject.SetBlinkObjectActive(visible);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (additionalGraphics == null || additionalGraphics.Length == 0)
                return;

            var targetState = state switch
            {
                SelectionState.Disabled => ButtonState.Disabled,
                SelectionState.Highlighted => ButtonState.Highlighted,
                SelectionState.Normal => ButtonState.Normal,
                SelectionState.Pressed => ButtonState.Pressed,
                SelectionState.Selected => ButtonState.Selected,
                _ => ButtonState.Normal
            };

            foreach (var graphic in additionalGraphics)
                graphic.TransitionToState(targetState, instant ? 0 : colors.fadeDuration);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            OnPointerEnterEvent?.Invoke();
            G.AudioService.PlaySound("click", pitchRandomness: 0.1f, volume: 0.4f);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            OnPointerExitEvent?.Invoke();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            G.AudioService.PlaySound("click", pitchRandomness: 0.1f);
            base.OnPointerClick(eventData);
        }

        public enum ButtonState
        {
            Disabled,
            Highlighted,
            Normal,
            Pressed,
            Selected
        }
    }
}