Shader "Pueblo de Sombras/Stylized River Water"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Color", 2D) = "white" {}
        [Normal] _NormalMap("Normal", 2D) = "bump" {}
        _DeepColor("Deep Color", Color) = (0.20, 0.31, 0.34, 1)
        _ShallowColor("Shallow Color", Color) = (0.46, 0.58, 0.59, 1)
        _FresnelColor("Fresnel Color", Color) = (0.68, 0.77, 0.78, 1)
        _Opacity("Opacity", Range(0.5, 1.0)) = 0.92
        _Tiling("Large Wave Tiling", Range(0.5, 20.0)) = 5.5
        _NormalStrength("Normal Strength", Range(0.0, 2.0)) = 0.42
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.72
        _SpeedA("Flow A", Vector) = (0.006, 0.002, 0, 0)
        _SpeedB("Flow B", Vector) = (-0.003, 0.005, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FresnelColor;
                float4 _SpeedA;
                float4 _SpeedB;
                float _Opacity;
                float _Tiling;
                float _NormalStrength;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half3 tangentWS : TEXCOORD3;
                half3 bitangentWS : TEXCOORD4;
                half fogFactor : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float2 uvA = input.uv * _Tiling + _SpeedA.xy * time;
                float2 uvB = input.uv * (_Tiling * 1.67) + _SpeedB.xy * time;

                half3 baseA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA).rgb;
                half3 baseB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB).rgb;
                half3 baseSample = lerp(baseA, baseB, 0.24h);

                half3 normalA = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvA),
                    _NormalStrength);
                half3 normalB = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvB),
                    _NormalStrength * 0.55h);
                half3 normalTS = normalize(half3(normalA.xy + normalB.xy * 0.55h, normalA.z));

                half3x3 tangentToWorld = half3x3(
                    normalize(input.tangentWS),
                    normalize(input.bitangentWS),
                    normalize(input.normalWS));
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                half luminance = dot(baseSample, half3(0.2126h, 0.7152h, 0.0722h));
                half paletteBlend = smoothstep(0.25h, 0.70h, luminance);
                half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, paletteBlend);
                color *= lerp(0.86h, 1.10h, paletteBlend);

                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = 0.62h + mainLight.color * (ndl * 0.48h);
                color *= lighting;

                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half specular = pow(saturate(dot(normalWS, halfDir)), lerp(24.0h, 112.0h, _Smoothness));
                color += mainLight.color * specular * (_Smoothness * 0.30h);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, input.positionWS);
                        half localNdl = saturate(dot(normalWS, light.direction));
                        color += light.color * (light.distanceAttenuation * localNdl * 0.10h);
                    }
                #endif

                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 4.0h);
                color = lerp(color, _FresnelColor.rgb, fresnel * 0.24h);
                color = MixFog(color, input.fogFactor);

                half alpha = saturate(_Opacity + fresnel * 0.05h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
