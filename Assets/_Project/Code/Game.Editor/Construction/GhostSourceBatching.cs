using Game.Scripts.Construction;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Game.Editor.Construction
{
    // A static-batched MeshFilter hands out the whole combined batch as its sharedMesh, so a site's ghost copied
    // from it shows half the level. The build batches inactive objects too, which the editor's play mode does not.
    public sealed class GhostSourceBatching : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var site in root.GetComponentsInChildren<ConstructionSiteAdapter>(true))
                {
                    var built = new SerializedObject(site).FindProperty("built").objectReferenceValue as GameObject;
                    if (built == null) continue;

                    foreach (var part in built.GetComponentsInChildren<Transform>(true))
                    {
                        var flags = GameObjectUtility.GetStaticEditorFlags(part.gameObject);
                        GameObjectUtility.SetStaticEditorFlags(part.gameObject, flags & ~StaticEditorFlags.BatchingStatic);
                    }
                }
            }
        }
    }
}
