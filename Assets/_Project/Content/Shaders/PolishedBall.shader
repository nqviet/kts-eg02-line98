Shader "Line98/PolishedBall"
{
    Properties
    {
        _BaseColor ("Gem color", Color) = (1, 0, 0, 1)
        [NoScaleOffset] _PatternTex ("Accessibility Pattern Texture", 2D) = "black" {}
        _PatternRect ("Pattern Rect (x=u, y=v, z=scaleU, w=scaleV)", Vector) = (0, 0, 1, 1)
        _PatternColor ("Pattern Color", Color) = (1, 1, 1, 1)
        _PatternStrength ("Pattern Strength", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalVS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _PatternRect;
                half4 _PatternColor;
                float _PatternStrength;
            CBUFFER_END

            TEXTURE2D(_PatternTex);
            SAMPLER(sampler_PatternTex);

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalVS = mul((float3x3)UNITY_MATRIX_V, TransformObjectToWorldNormal(v.normalOS));
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalVS);
                float facing = saturate(n.z);
                float key = saturate(dot(n, normalize(float3(-.45, .65, .65))));
                half3 color = _BaseColor.rgb * (.22 + .72 * key + .15 * facing);
                float coat = pow(saturate(dot(n, normalize(float3(-.32, .42, .85)))), 55);
                float bloom = pow(saturate(dot(n, normalize(float3(-.32, .42, .85)))), 12);
                color = lerp(color, half3(1, 1, 1), saturate(coat * .9 + bloom * .16));
                color += _BaseColor.rgb * pow(1 - facing, 4) * .2;

                if (_PatternStrength > 0.001)
                {
                    float2 patUV = _PatternRect.xy + i.uv * _PatternRect.zw;
                    half4 patSample = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, patUV);
                    half mask = saturate(patSample.a * (patSample.r + patSample.g + patSample.b) * 0.3333);
                    half patAlpha = mask * _PatternStrength;
                    color = lerp(color, _PatternColor.rgb, patAlpha * 0.85);
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
