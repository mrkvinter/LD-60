using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>
    /// Sways the on-screen hand while the player walks: side to side once per stride, a dip on every step. The pace
    /// comes from how fast the camera moves over the ground, so it follows walking, sprinting and crouching alike.
    /// </summary>
    public sealed class HandBob : MonoBehaviour
    {
        private const float FullCircle = Mathf.PI * 2f;

        [SerializeField] private RectTransform target;

        [Tooltip("Sway to the side and dip down at full speed, in canvas pixels.")]
        [SerializeField] private Vector2 amplitude = new(14f, 10f);

        [Tooltip("Meters walked per full sway, there and back: two steps.")]
        [SerializeField, Min(0.1f)] private float strideLength = 2.2f;

        [Tooltip("Speed at which the sway reaches full amplitude, in meters per second.")]
        [SerializeField, Min(0.1f)] private float fullSpeed = 4f;

        [Tooltip("How fast the sway fades in when walking starts and settles when it stops, per second.")]
        [SerializeField, Min(0.1f)] private float blendSpeed = 6f;

        private Vector2 basePosition;
        private Vector3 lastViewPosition;
        private bool hasLastPosition;
        private float phase;
        private float weight;

        private void Reset()
        {
            target = transform as RectTransform;
        }

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
            basePosition = target.anchoredPosition;
        }

        private void LateUpdate()
        {
            var view = Camera.main;
            if (view == null || Time.deltaTime <= 0f) return;

            var position = view.transform.position;
            var step = position - lastViewPosition;
            step.y = 0f;
            var speed = hasLastPosition ? step.magnitude / Time.deltaTime : 0f;
            lastViewPosition = position;
            hasLastPosition = true;

            // A teleport is not a step.
            if (speed > fullSpeed * 4f) speed = 0f;

            weight = Mathf.MoveTowards(weight, Mathf.Clamp01(speed / fullSpeed), blendSpeed * Time.deltaTime);
            phase = (phase + speed / strideLength * FullCircle * Time.deltaTime) % FullCircle;

            var sway = Mathf.Sin(phase);
            target.anchoredPosition = basePosition + new Vector2(sway * amplitude.x, -Mathf.Abs(sway) * amplitude.y) * weight;
        }
    }
}
