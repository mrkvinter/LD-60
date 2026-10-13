using UnityEngine;

namespace Code.UI
{
    public class BlinkAdditionalGraphics : BaseAdditionalGraphics
    {
        [SerializeField] private GameObject blinkObject;
        
        public void SetBlinkObjectActive(bool isActive)
        {
            blinkObject.SetActive(isActive);
        }

        public override void TransitionToState(MyButton.ButtonState state, float fadeDuration)
        {
        }
    }
}