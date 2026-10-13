using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI
{
    public class AdditionalGraphics : BaseAdditionalGraphics
    {
        [SerializeField] private Graphic graphic;
        [SerializeField] private Color disabledColor = Color.grey;
        [SerializeField] private Color highlightedColor = Color.white;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = Color.white;
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField, MinMaxSlider(1, 5)] private float colorMultiplier = 1;

        public override void TransitionToState(MyButton.ButtonState state, float fadeDuration)
        {
            if (graphic == null)
                return;

            var targetColor = state switch
            {
                MyButton.ButtonState.Disabled => disabledColor,
                MyButton.ButtonState.Highlighted => highlightedColor,
                MyButton.ButtonState.Normal => normalColor,
                MyButton.ButtonState.Pressed => pressedColor,
                MyButton.ButtonState.Selected => selectedColor,
                _ => Color.white
            };

            graphic.CrossFadeColor(targetColor * colorMultiplier, fadeDuration, true, true);
        }
    }
}