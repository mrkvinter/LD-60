using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Code.Game.Editor.ArtPipeline
{
    public static class ArtMaterialTools
    {
        [MenuItem("Game/Art/Convert Selected Materials to Body Shader")]
        static void ConvertSelected()
        {
            var materials = Selection.GetFiltered<Material>(SelectionMode.DeepAssets);
            if (materials.Length == 0)
            {
                Debug.LogWarning("[ArtPipeline] No materials selected.");
                return;
            }

            Convert(materials);
        }

        [MenuItem("Game/Art/Convert All Art Materials to Body Shader")]
        static void ConvertAll()
        {
            var materials = AssetDatabase
                .FindAssets("t:Material", new[] { ArtPipelineConventions.ArtRoot.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .Where(m => m != null)
                .ToArray();

            if (materials.Length == 0)
                return;

            var proceed = EditorUtility.DisplayDialog(
                "Convert art materials",
                $"Switch {materials.Length} material(s) under {ArtPipelineConventions.ArtRoot} to '{ArtPipelineConventions.BodyShaderName}'?\n\n" +
                "Base map and tint are carried over; every other property resets to the shader default.",
                "Convert",
                "Cancel");

            if (proceed)
                Convert(materials);
        }

        static void Convert(IReadOnlyList<Material> materials)
        {
            if (!ArtPipelineConventions.TryGetBodyShader(out var shader))
                return;

            var converted = 0;
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material.shader == shader)
                    continue;

                var baseMap = material.mainTexture;
                var baseColor = material.HasProperty(ArtPipelineConventions.BaseColor) || material.HasProperty("_Color")
                    ? material.color
                    : Color.white;

                Undo.RecordObject(material, "Convert to Body Shader");
                material.shader = shader;
                material.SetTexture(ArtPipelineConventions.BaseMap, baseMap);
                material.SetColor(ArtPipelineConventions.BaseColor, baseColor);
                EditorUtility.SetDirty(material);
                converted++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ArtPipeline] Converted {converted} material(s) to '{ArtPipelineConventions.BodyShaderName}'.");
        }
    }
}
