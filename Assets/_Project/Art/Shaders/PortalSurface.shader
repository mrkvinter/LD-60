// The portal's opening: a disc of rings sliding inward like a tunnel, a swirl of noise, a bright rim and twinkling
// stars over a dark core. Unlit and transparent; the glow comes from an HDR color, so the scene's Bloom picks it up.
// The mesh needs UVs that fill 0..1 with the disc's middle at (0.5, 0.5). Open (0..1) grows the disc from its middle.
Shader "Custom/PortalSurface"
{
    Properties
    {
        [HDR] _GlowColor("Glow Color", Color) = (1, 1, 1, 1)
        _CoreColor("Core Color", Color) = (0, 0, 0, 1)
        _Open("Open", Range(0, 1)) = 1

        [Header(Shape)]
        _Radius("Radius", Range(0.1, 1)) = 0.92
        _EdgeSoftness("Edge Softness", Range(0.001, 0.2)) = 0.03
        _RimStrength("Rim Strength", Range(0, 2)) = 1

        [Header(Rings)]
        _RingCount("Ring Count", Float) = 6
        _RingSpeed("Ring Speed", Float) = 0.3
        _RingWidth("Ring Width", Range(0.01, 1)) = 0.15

        [Header(Swirl)]
        _SwirlStrength("Swirl Strength", Range(0, 2)) = 0.6
        _SwirlSpeed("Swirl Speed", Float) = 0.3
        _Twist("Twist", Float) = 4
        _NoiseScale("Noise Scale", Float) = 4

        [Header(Stars)]
        _StarDensity("Star Density", Float) = 25
        _StarAmount("Star Amount", Range(0, 1)) = 0.3
        _StarSize("Star Size", Range(0.01, 0.5)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Portal"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex PortalVertex
            #pragma fragment PortalFragment
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _GlowColor;
            half4 _CoreColor;
            half _Open;
            half _Radius;
            half _EdgeSoftness;
            half _RimStrength;
            float _RingCount;
            float _RingSpeed;
            half _RingWidth;
            half _SwirlStrength;
            float _SwirlSpeed;
            float _Twist;
            float _NoiseScale;
            float _StarDensity;
            half _StarAmount;
            half _StarSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int octave = 0; octave < 3; octave++)
                {
                    value += amplitude * ValueNoise(p);
                    p *= 2.02;
                    amplitude *= 0.5;
                }
                return value;
            }

            float2 Rotate(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            // One possible star per grid cell, in a random spot, blinking at its own pace.
            float Stars(float2 p, float time)
            {
                float2 grid = p * _StarDensity;
                float2 cell = floor(grid);
                float2 spot = float2(Hash21(cell), Hash21(cell + 17.3)) * 0.8 + 0.1;
                float star = saturate(1.0 - length(frac(grid) - spot) / _StarSize);
                float present = step(1.0 - _StarAmount, Hash21(cell + 9.7));
                float twinkle = 0.5 + 0.5 * sin(time * 3.0 + Hash21(cell + 3.1) * 6.2832);
                return star * star * present * twinkle;
            }

            Varyings PortalVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 PortalFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float time = _Time.y;
                float2 delta = input.uv - 0.5;

                // 0 in the middle, 1 at the disc's edge; the edge itself shrinks to the middle as Open goes to 0.
                float radius = max(_Radius * _Open, 0.0001);
                float r = length(delta) * 2.0;
                float t = r / radius;
                float inside = 1.0 - smoothstep(1.0 - _EdgeSoftness / radius, 1.0, t);

                // Rings slide inward and fade toward the middle, which reads as depth.
                float rings = frac(t * _RingCount + time * _RingSpeed);
                rings = smoothstep(1.0 - _RingWidth, 1.0, rings) * smoothstep(0.1, 0.8, t);

                // Spin the whole pattern, twisting the middle more than the rim.
                float2 swirled = Rotate(delta, time * _SwirlSpeed + _Twist * (1.0 - saturate(t)));
                float swirl = Fbm(swirled * _NoiseScale + 3.7);
                swirl = swirl * swirl * smoothstep(0.15, 0.9, t);

                float rim = smoothstep(0.75, 1.0, t) * _RimStrength;

                float glow = saturate(rings + swirl * _SwirlStrength + rim);
                float stars = Stars(Rotate(delta, time * _SwirlSpeed * 0.5), time) * (1.0 - glow);

                half3 color = lerp(_CoreColor.rgb, _GlowColor.rgb, glow) + _GlowColor.rgb * stars;
                half alpha = inside * max(_CoreColor.a, glow);

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
