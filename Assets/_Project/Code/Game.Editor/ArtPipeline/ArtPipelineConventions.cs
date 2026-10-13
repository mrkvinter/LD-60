using UnityEngine;

namespace Code.Game.Editor.ArtPipeline
{
    public static class ArtPipelineConventions
    {
        public const string ArtRoot = "Assets/_Project/Art/";
        public const string BodyShaderName = "Darkcracy/Body";

        public static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static bool IsOwnArt(string assetPath) => assetPath.StartsWith(ArtRoot, System.StringComparison.Ordinal);

        public static bool TryGetBodyShader(out Shader shader)
        {
            shader = Shader.Find(BodyShaderName);
            if (shader != null)
                return true;

            Debug.LogError($"[ArtPipeline] Shader '{BodyShaderName}' not found; materials keep their current shader.");
            return false;
        }
    }
}
