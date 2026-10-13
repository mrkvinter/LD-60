using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Code.Game.Editor.ArtPipeline
{
    public sealed class ArtImportPostprocessor : AssetPostprocessor
    {
        // URP ставит свой Lit на порядке -980; наш шейдер должен перекрыть его, а не наоборот.
        public override int GetPostprocessOrder() => 100;

        void OnPreprocessTexture()
        {
            if (!ArtPipelineConventions.IsOwnArt(assetPath))
                return;

            ((TextureImporter)assetImporter).filterMode = FilterMode.Point;
        }

        void OnPreprocessModel()
        {
            var importer = (ModelImporter)assetImporter;

            // Только на первом импорте: у уже настроенных моделей ремапы материалов
            // лежат в .meta, и переключение режима на каждом реимпорте их перетирает.
            if (!ArtPipelineConventions.IsOwnArt(assetPath) || !importer.importSettingsMissing)
                return;

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.External;
        }

        void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (!ArtPipelineConventions.IsOwnArt(assetPath))
                return;

            if (!ArtPipelineConventions.TryGetBodyShader(out var shader))
                return;

            material.shader = shader;

            if (description.TryGetProperty("DiffuseColor", out TexturePropertyDescription diffuseMap) && diffuseMap.texture != null)
            {
                material.SetTexture(ArtPipelineConventions.BaseMap, diffuseMap.texture);
                material.SetTextureOffset(ArtPipelineConventions.BaseMap, diffuseMap.offset);
                material.SetTextureScale(ArtPipelineConventions.BaseMap, diffuseMap.scale);
            }
            else if (description.TryGetProperty("DiffuseColor", out Vector4 diffuseColor))
            {
                material.SetColor(ArtPipelineConventions.BaseColor, diffuseColor);
            }
        }
    }
}
