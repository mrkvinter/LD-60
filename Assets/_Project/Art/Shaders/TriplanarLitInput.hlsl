#ifndef TRIPLANAR_LIT_INPUT_INCLUDED
#define TRIPLANAR_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

// Same layout in every pass, or the SRP Batcher drops the material.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
float _TileSize;
half _BlendSharpness;
half _Smoothness;
half _Metallic;
CBUFFER_END

float TriplanarSign(float value)
{
    return value < 0.0 ? -1.0 : 1.0;
}

// The texture is projected along the world axes, so it keeps one size in meters on any wall, whatever its scale or UVs.
half4 SampleTriplanar(float3 positionWS, float3 normalWS)
{
    float3 position = positionWS / max(_TileSize, 0.001);

    half3 weights = pow(abs(normalWS), _BlendSharpness);
    weights /= max(weights.x + weights.y + weights.z, 0.0001);

    // Flipped on the opposite faces, so the texture doesn't read mirrored from either side.
    float2 uvX = float2(position.z * TriplanarSign(normalWS.x), position.y);
    float2 uvY = float2(position.x * TriplanarSign(normalWS.y), position.z);
    float2 uvZ = float2(-position.x * TriplanarSign(normalWS.z), position.y);

    half4 x = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(uvX, _BaseMap));
    half4 y = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(uvY, _BaseMap));
    half4 z = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(uvZ, _BaseMap));

    return x * weights.x + y * weights.y + z * weights.z;
}

void InitializeTriplanarSurfaceData(float3 positionWS, float3 normalWS, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedo = SampleTriplanar(positionWS, normalize(normalWS));
    outSurfaceData.albedo = albedo.rgb * _BaseColor.rgb;
    outSurfaceData.alpha = 1.0;
    outSurfaceData.metallic = _Metallic;
    outSurfaceData.smoothness = _Smoothness;
    outSurfaceData.normalTS = half3(0.0, 0.0, 1.0);
    outSurfaceData.occlusion = 1.0;
}

#endif
