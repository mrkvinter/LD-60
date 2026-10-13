using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Game.Editor
{
    [InitializeOnLoad]
    public class CanvasLayoutRebuilder
    {
        // A unique key to store the session state.
        private const string FirstLoadKey = "CanvasLayoutRebuilder_FirstLoadDone";

        static CanvasLayoutRebuilder()
        {
            // Check a session-specific flag. If it's false, this is the first load.
            if (!SessionState.GetBool(FirstLoadKey, false))
            {
                // Set the flag to true immediately to prevent this from running again
                // after a script recompile within the same session.
                SessionState.SetBool(FirstLoadKey, true);
                
                // Perform the scene reload after a short delay.
                EditorApplication.delayCall += ReloadScene;
            }
        }
        
        private static void ReloadScene()
        {
            // It's still good practice to keep these safety checks.
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                // If the editor is busy, try again in the next frame.
                EditorApplication.delayCall += ReloadScene;
                return;
            }
            
            string scenePath = SceneManager.GetActiveScene().path;
            
            if (!string.IsNullOrEmpty(scenePath))
            {
                Debug.Log($"[CanvasLayoutRebuilder] Automatically reloading scene once on editor launch: {scenePath}");
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }
    }
}