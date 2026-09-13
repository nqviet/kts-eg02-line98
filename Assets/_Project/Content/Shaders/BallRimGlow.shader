Shader "Line98/BallRimGlow"
{
    Properties
    {
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 10.0)) = 3.5
        _RimIntensity ("Rim Intensity", Range(0.0, 5.0)) = 1.0
        _PulseSpeed ("Pulse Speed (Hz)", Float) = 1.111
        _PulseDepth ("Pulse Depth", Range(0.0, 1.0)) = 0.35
        [HideInInspector] _ZWrite ("__zw", Float) = 0.0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "BallRimGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _PulseSpeed;
                float _PulseDepth;
                float _ZWrite;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float NdotV = saturate(dot(normalWS, viewDirWS));
                float breathe = sin(_Time.y * _PulseSpeed * 6.2831853);
                float rimPower = max(0.1, _RimPower * (1.0 - _PulseDepth * 0.5 * breathe));
                float rim = pow(saturate(1.0 - NdotV), rimPower);

                half3 color = _RimColor.rgb * _RimIntensity * (1.0 + _PulseDepth * breathe);
                return half4(color, rim * _RimColor.a);
            }
            ENDHLSL
        }
    }
}
