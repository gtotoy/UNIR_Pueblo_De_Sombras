Shader "Pueblo de Sombras/Low Ground Fog"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.31, 0.26, 0.36, 1)
        _Opacity("Opacity", Range(0, 0.5)) = 0.14
        _NoiseScale("Noise Scale", Range(0.002, 0.08)) = 0.018
        _Speed("Drift Speed", Vector) = (0.10, 0.06, 0, 0)
        _EdgeFeather("Edge Feather", Range(0.02, 0.48)) = 0.22
        _SoftIntersection("Soft Intersection", Range(0.1, 12)) = 5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+80"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "GroundFog"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
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
                float4 _FogColor;
                float4 _Speed;
                float _Opacity;
                float _NoiseScale;
                float _EdgeFeather;
                float _SoftIntersection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                half fogFactor : TEXCOORD3;
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
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FogNoise(float2 p)
            {
                float result = ValueNoise(p) * 0.58;
                result += ValueNoise(p * 2.07 + 11.7) * 0.28;
                result += ValueNoise(p * 4.13 - 4.2) * 0.14;
                return result;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(positions.positionCS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 drift = _Time.y * _Speed.xy * 0.10;
                float2 noiseUV = input.positionWS.xz * _NoiseScale + drift;
                float noise = FogNoise(noiseUV);
                float detail = FogNoise(noiseUV * 0.53 - drift * 0.35 + 17.0);
                float body = smoothstep(0.26, 0.76, noise * 0.72 + detail * 0.28);

                float2 edgeDistance = min(input.uv, 1.0 - input.uv);
                float edgeFade = smoothstep(0.0, _EdgeFeather, edgeDistance.x)
                               * smoothstep(0.0, _EdgeFeather, edgeDistance.y);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawSceneDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float fogDepth = input.screenPos.w;
                float intersectionFade = saturate((sceneDepth - fogDepth) / max(_SoftIntersection, 0.001));

                half alpha = (half)(_Opacity * edgeFade * intersectionFade * lerp(0.48, 1.0, body));
                half3 color = _FogColor.rgb * lerp(0.78h, 1.08h, (half)body);
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
