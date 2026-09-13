Shader "Line98/PolishedBall"
{
    Properties { _BaseColor ("Gem color", Color) = (1,0,0,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalVS : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalVS = mul((float3x3)UNITY_MATRIX_V, TransformObjectToWorldNormal(v.normalOS));
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalVS);
                float facing = saturate(n.z);
                float key = saturate(dot(n, normalize(float3(-.45,.65,.65))));
                half3 color = _BaseColor.rgb * (.22 + .72 * key + .15 * facing);
                float coat = pow(saturate(dot(n,normalize(float3(-.32,.42,.85)))),55);
                float bloom = pow(saturate(dot(n,normalize(float3(-.32,.42,.85)))),12);
                color = lerp(color, half3(1,1,1), saturate(coat*.9 + bloom*.16));
                color += _BaseColor.rgb * pow(1-facing,4) * .2;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
