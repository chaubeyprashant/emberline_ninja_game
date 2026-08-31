// Additive glow with a procedural radial falloff — no texture needed.
// Used by particles, sword trails, and lantern halos. Supports vertex color.
Shader "Emberline/Glow"
{
    Properties
    {
        _Color ("Color", Color) = (1,0.55,0.3,1)
        _Soft ("Softness", Range(0.5,4)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Soft;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float d = distance(i.uv, float2(0.5, 0.5)) * 2.0;
                float a = pow(saturate(1.0 - d), _Soft);
                return fixed4(i.color.rgb, i.color.a * a);
            }
            ENDCG
        }
    }
}
