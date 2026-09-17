Shader "Line98/CrystalBall"
{
    // Crystal theme gem: glass shell with a darker rim, a lit inner core, a sharp window
    // highlight and a bottom caustic. Unlit and view-space like PolishedBall, with the same
    // property layout so accessibility patterns (D33) and SRP batching behave identically.
    Properties
    {
        _BaseColor ("Gem color", Color) = (1, 0, 0, 1)
        [NoScaleOffset] _PatternTex ("Accessibility Pattern Texture", 2D) = "black" {}
        _PatternRect ("Pattern Rect (x=u, y=v, z=scaleU, w=scaleV)", Vector) = (0, 0, 1, 1)
        _PatternColor ("Pattern Color", Color) = (1, 1, 1, 1)
        _PatternStrength ("Pattern Strength", Range(0.0, 1.0)) = 0.0
        _Depth ("Color Depth", Range(1.0, 2.5)) = 1.6
        _CoreRadius ("Inner Core Radius", Range(0.3, 0.9)) = 0.64
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
                float _Depth;
                float _CoreRadius;
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
                float edge = 1.0 - facing;

                // Deeper, more saturated body than the raw palette swatch; the palette hue is preserved.
                half3 deep = pow(_BaseColor.rgb, _Depth);
                half3 bright = lerp(_BaseColor.rgb, half3(1, 1, 1), 0.18);

                // Glass shell: saturated body darkening toward the silhouette.
                half3 color = lerp(_BaseColor.rgb * 0.9, deep * 0.45, smoothstep(0.25, 0.95, edge));

                // Inner core sphere, offset slightly down-right, lit from the upper left.
                float2 corePos = (n.xy - float2(0.04, -0.06)) / _CoreRadius;
                float coreR2 = dot(corePos, corePos);
                if (coreR2 < 1.0)
                {
                    float3 coreN = float3(corePos, sqrt(1.0 - coreR2));
                    float coreKey = saturate(dot(coreN, normalize(float3(-0.5, 0.6, 0.62))));
                    half3 coreColor = lerp(deep * 0.7, bright, coreKey * 0.6);
                    float coreSpec = pow(saturate(dot(coreN, normalize(float3(-0.25, 0.35, 0.9)))), 60);
                    coreColor = lerp(coreColor, half3(1, 1, 1), coreSpec * 0.55);
                    float coreEdge = smoothstep(1.0, 0.82, coreR2);
                    color = lerp(color * 0.8, coreColor, coreEdge);
                }

                // Bottom caustic: light refracted through the gem pools opposite the key light.
                float caustic = smoothstep(0.15, 0.75, dot(normalize(n.xy + 1e-4), float2(0.35, -0.94)))
                              * smoothstep(0.1, 0.55, edge) * smoothstep(1.0, 0.8, edge);
                color = lerp(color, bright, caustic * 0.45);

                // Window highlight on the shell (upper left): broad glossy pane plus a sharp hot spot.
                float3 windowDir = normalize(float3(-0.45, 0.55, 0.7));
                float window = smoothstep(0.86, 0.93, dot(n, windowDir));
                float hotSpot = pow(saturate(dot(n, normalize(float3(-0.38, 0.46, 0.8)))), 90);
                color = lerp(color, half3(1, 1, 1), saturate(window * 0.4 + hotSpot));

                // Thin bright glass lip along the silhouette.
                float lip = smoothstep(0.82, 0.97, edge) * smoothstep(1.0, 0.97, edge);
                color += bright * lip * 0.25;

                if (_PatternStrength > 0.001)
                {
                    float2 patUV = _PatternRect.xy + i.uv * _PatternRect.zw;
                    half4 patSample = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, patUV);
                    half mask = saturate(patSample.a * (patSample.r + patSample.g + patSample.b) * 0.3333);
                    half patAlpha = mask * _PatternStrength;
                    color = lerp(color, _PatternColor.rgb, patAlpha * 0.85);
                }

                return half4(saturate(color), 1);
            }
            ENDHLSL
        }
    }
}
