Shader "Pueblo de Sombras/Air Particles"
{
    Properties
    {
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _Opacity("Opacity", Range(0, 2)) = 1
        _Shape("Shape: Mote to Ash", Range(0, 1)) = 0
        _SoftDistance("Soft Intersection", Range(0.01, 5)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+120"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "AirParticles"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Opacity;
                float _Shape;
                float _SoftDistance;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.color = input.color;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(positions.positionCS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float moteDistance = length(p);
                float moteMask = smoothstep(1.0, 0.18, moteDistance);
                moteMask *= lerp(0.72, 1.0, smoothstep(0.72, 0.0, moteDistance));

                float2 ashP = float2(p.x * 1.75, p.y * 0.68);
                float irregularity = sin((p.x * 9.0 + p.y * 13.0) * 2.1) * 0.06;
                float ashMask = smoothstep(1.0, 0.42, length(ashP) + irregularity);
                float shapeMask = lerp(moteMask, ashMask, saturate(_Shape));

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float particleDepth = input.screenPos.w;
                float depthFade = saturate((sceneDepth - particleDepth) / max(_SoftDistance, 0.001));
                float softFade = lerp(0.58, 1.0, depthFade);

                half alpha = input.color.a * _Tint.a * (half)(_Opacity * shapeMask * softFade);
                half3 color = input.color.rgb * _Tint.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
