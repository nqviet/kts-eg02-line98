Shader "Line98/FrostedPanel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size ("Size", Vector) = (318,176,0,0)
        _Radius ("Corner radius", Float) = 32
        _Border ("Border width", Float) = 2.5
        _Fill ("Glass tint", Color) = (0.8,0.9,1,0.5)
        _Rim ("Rim", Color) = (1,1,1,0.95)
        _Inset ("Recess shading", Float) = 0
        _Padding ("Shadow padding", Float) = 0
        _Feather ("Shadow feather", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Size;
                half4 _Fill, _Rim;
                float _Radius, _Border, _Inset, _Padding, _Feather;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * _Size.xy;
                float2 q = abs(p) - _Size.xy * 0.5 + _Padding + _Radius;
                float d = length(max(q, 0)) + min(max(q.x, q.y), 0) - _Radius;
                float aa = max(fwidth(d), 0.001);
                float coverage = 1 - smoothstep(-aa, max(_Feather,0.001), d);
                float rim = smoothstep(-_Border-aa, -_Border+aa, d);
                half4 fill = _Fill;
                fill.rgb *= 0.97 + 0.03 * i.uv.y;
                float cavity = exp(min(d, 0) / max(_Size.y * 0.045, 0.001));
                fill.rgb *= 1 - _Inset * cavity * (0.45 + 0.55 * i.uv.y);
                half4 c = lerp(fill, _Rim, rim * step(0.001, _Border));
                c.a *= coverage;
                return c * i.color;
            }
            ENDHLSL
        }
    }
}
