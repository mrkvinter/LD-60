using UnityEngine;

namespace Game.Main
{
    public class SceneLinks : MonoBehaviour
    {
        [SerializeField] private Canvas canvasGame;
        [Tooltip("_CANVAS Main: the camera-space canvas showing the gameplay render texture. Widgets targeting CanvasTarget.Main land here so the camera's post-processing (pixelation, VHS) applies to them.")]
        [SerializeField] private Canvas canvasMain;

        public Canvas CanvasGame => canvasGame;
        public Canvas CanvasMain => canvasMain;
    }
}
