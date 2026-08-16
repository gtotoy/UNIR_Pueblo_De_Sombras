Shader "Custom/LockOnRing"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.85, 0.2, 1)
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.32
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.45
        _Softness ("Edge Softness", Range(0.001, 0.2)) = 0.02
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _InnerRadius;
            float _OuterRadius;
            float _Softness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dist = distance(i.uv, float2(0.5, 0.5));
                float outerEdge = smoothstep(_OuterRadius, _OuterRadius - _Softness, dist);
                float innerEdge = smoothstep(_InnerRadius, _InnerRadius + _Softness, dist);
                float ring = saturate(outerEdge * innerEdge);
                return fixed4(_Color.rgb, _Color.a * ring);
            }
            ENDCG
        }
    }
}
