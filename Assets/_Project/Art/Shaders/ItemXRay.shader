// Drawn on top of an item as an extra material: unlit, seen through everything, a glowing rim with a soft pulse.
Shader "Game/Item X-Ray"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.3, 0.85, 1, 0.7)
        _Fill ("Fill", Range(0, 1)) = 0.25
        _RimPower ("Rim Power", Range(0.5, 8)) = 2
        _PulseSpeed ("Pulse Speed", Float) = 4
        _PulseDepth ("Pulse Depth", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "XRay"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest Always
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Fill;
                half _RimPower;
                half _PulseSpeed;
                half _PulseDepth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half facing = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half rim = pow(1.0h - facing, _RimPower);
                half pulse = 1.0h - _PulseDepth * (0.5h + 0.5h * sin(_Time.y * _PulseSpeed));
                return half4(_Color.rgb, saturate(_Color.a * (_Fill + rim) * pulse));
            }
            ENDHLSL
        }
    }
}
