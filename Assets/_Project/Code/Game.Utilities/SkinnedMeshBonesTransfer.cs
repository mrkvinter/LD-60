using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Game.Editor
{
    public class SkinnedMeshBonesTransfer : MonoBehaviour
    {
        public SkinnedMeshRenderer sourceMeshRenderer;
        public Transform[] bones;

        [Button]
        public void Transfer()
        {
            var targetMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (sourceMeshRenderer == null || targetMeshRenderer == null)
            {
                Debug.LogError("Both a source renderer and a SkinnedMeshRenderer on this object are required.", this);
                return;
            }

            var boneNames = ResolveBoneNames(targetMeshRenderer);
            if (boneNames == null)
            {
                Debug.LogError("Cannot tell which bones this mesh needs: the imported model was not found and the " +
                               "current bone list is broken. Re-add the mesh from its FBX and try again.", this);
                return;
            }

            var skeleton = BuildSkeletonLookup();
            var rebound = new Transform[boneNames.Length];
            var missing = new List<string>();

            for (var i = 0; i < boneNames.Length; i++)
            {
                if (skeleton.TryGetValue(boneNames[i], out var match))
                    rebound[i] = match;
                else
                    missing.Add(boneNames[i]);
            }

            if (missing.Count > 0)
            {
                Debug.LogError($"Source skeleton has no bones named: {string.Join(", ", missing)}.", this);
                return;
            }

            targetMeshRenderer.bones = rebound;
            if (sourceMeshRenderer.rootBone != null && skeleton.TryGetValue(sourceMeshRenderer.rootBone.name, out var root))
                targetMeshRenderer.rootBone = root;
            bones = rebound;

            Debug.Log($"Rebound {rebound.Length} bones of '{name}' to the skeleton of '{sourceMeshRenderer.name}'.", this);
        }

        private string[] ResolveBoneNames(SkinnedMeshRenderer targetMeshRenderer)
        {
#if UNITY_EDITOR
            var fromModel = ReadBoneNamesFromModel(targetMeshRenderer);
            if (fromModel != null)
                return fromModel;
#endif
            var current = targetMeshRenderer.bones;
            var names = new string[current.Length];
            for (var i = 0; i < current.Length; i++)
            {
                if (current[i] == null)
                    return null;
                names[i] = current[i].name;
            }

            return names.Length > 0 ? names : null;
        }

#if UNITY_EDITOR
        private static string[] ReadBoneNamesFromModel(SkinnedMeshRenderer targetMeshRenderer)
        {
            var mesh = targetMeshRenderer.sharedMesh;
            if (mesh == null)
                return null;

            var path = UnityEditor.AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path))
                return null;

            foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is GameObject root))
                    continue;

                foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.sharedMesh != mesh)
                        continue;

                    var modelBones = renderer.bones;
                    var names = new string[modelBones.Length];
                    for (var i = 0; i < modelBones.Length; i++)
                    {
                        if (modelBones[i] == null)
                            return null;
                        names[i] = modelBones[i].name;
                    }

                    return names;
                }
            }

            return null;
        }
#endif

        private Dictionary<string, Transform> BuildSkeletonLookup()
        {
            var lookup = new Dictionary<string, Transform>();

            var root = sourceMeshRenderer.rootBone;
            if (root != null)
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    lookup[transform.name] = transform;

            foreach (var bone in sourceMeshRenderer.bones)
                if (bone != null)
                    lookup[bone.name] = bone;

            return lookup;
        }
    }
}
