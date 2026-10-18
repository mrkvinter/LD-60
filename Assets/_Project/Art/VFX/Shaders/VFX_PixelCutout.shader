Shader "VFX/PixelCutout"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture (Point)", 2D) = "white" {}
        [HDR] _BaseColor ("Tint", Color) = (1,1,1,1)
        _Boost ("Brightness Boost", Range(0, 8)) = 1
        _Cutoff ("Texture Alpha Cutoff", Range(0, 1)) = 0.5
        _DitherPixelSize ("Dither Cell (screen px)", Range(1, 4)) = 2
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest+50" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Boost;
                half _Cutoff;
                half _DitherPixelSize;
                half _Cull;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            half Bayer8(uint2 p)
            {
                uint a = (p.x ^ p.y) & 7u;
                uint b = p.y & 7u;
                uint v = ((a & 1u) << 5) | ((b & 1u) << 4) | ((a & 2u) << 2) | ((b & 2u) << 1) | ((a & 4u) >> 1) | ((b & 4u) >> 2);
                return v * (1.0h / 64.0h);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(tex.a - _Cutoff);
                // Vertex alpha is a coverage fade: pixels drop out on a Bayer grid instead of blending,
                // so a fading particle loses pixels like the rest of the palette-dithered frame.
                uint2 cell = (uint2)(i.positionCS.xy / max(_DitherPixelSize, 1.0h));
                clip(i.color.a * _BaseColor.a - Bayer8(cell) - 0.001h);
                half3 rgb = tex.rgb * i.color.rgb * _BaseColor.rgb * _Boost;
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
