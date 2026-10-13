Shader "Custom/DitherFadeing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DitherSize("Dither Size", Float) = 0.1
        _DitherIntensity("Dither Intensity", Float) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue"="Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual


            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _DitherSize;
            float _DitherIntensity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; 
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float luma(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float luma(float4 color)
            {
                return dot(color.rgb, float3(0.299, 0.587, 0.114));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            float dither4x4(float2 position, float brightness)
            {
                int x = int(fmod(position.x, 4.0));
                int y = int(fmod(position.y, 4.0));
                int index = x + y * 4;
                float limit = 0.0;

                if (x < 8)
                {
                    if (index == 0) limit = 0.0625;
                    if (index == 1) limit = 0.5625;
                    if (index == 2) limit = 0.1875;
                    if (index == 3) limit = 0.6875;
                    if (index == 4) limit = 0.8125;
                    if (index == 5) limit = 0.3125;
                    if (index == 6) limit = 0.9375;
                    if (index == 7) limit = 0.4375;
                    if (index == 8) limit = 0.25;
                    if (index == 9) limit = 0.75;
                    if (index == 10) limit = 0.125;
                    if (index == 11) limit = 0.625;
                    if (index == 12) limit = 1.0;
                    if (index == 13) limit = 0.5;
                    if (index == 14) limit = 0.875;
                    if (index == 15) limit = 0.375;
                }

                return brightness < limit ? 0.0 : 1.0;
            }

            float3 dither4x4(float2 position, float3 color)
            {
                return color * dither4x4(position, luma(color));
            }

            float4 dither4x4(float2 position, float4 color)
            {
                return float4(color.rgb * dither4x4(position, luma(color)), 1.0);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 pixelUV = uv;
                half4 color = tex2D(_MainTex, pixelUV) * input.color;
                float b = dither4x4(input.positionCS.xy * _DitherSize, _DitherIntensity);
                color.a *= b;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}