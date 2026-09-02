Shader "Pueblo de Sombras/Lightning Bolt"
{
    Properties
    {
        [HDR] _Tint("Tint", Color) = (1.4, 1.2, 2.4, 1)
        _Opacity("Opacity", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+180" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "LightningBolt"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float edge = smoothstep(0.0, 0.22, input.uv.y) * smoothstep(0.0, 0.22, 1.0 - input.uv.y);
                half alpha = input.color.a * _Tint.a * (half)(_Opacity * edge);
                return half4(input.color.rgb * _Tint.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
