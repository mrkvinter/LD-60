using UnityEngine;

namespace Code.UI
{
    public abstract class BaseAdditionalGraphics : MonoBehaviour
    {
        public abstract void TransitionToState(MyButton.ButtonState state, float fadeDuration);
    }
}