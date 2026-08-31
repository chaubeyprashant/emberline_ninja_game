// Cel-shaded character/prop shader with inverted-hull ink outline.
// Built-in render pipeline, mobile-cheap: one directional light, 3-band ramp, rim.
Shader "Emberline/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowTint ("Shadow Tint", Color) = (0.42,0.47,0.60,1)
        _RimColor ("Rim Color", Color) = (0.30,0.40,0.52,1)
        _OutlineColor ("Outline Color", Color) = (0.04,0.05,0.08,1)
        _OutlineWidth ("Outline Width", Float) = 0.014
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // ---- outline pass (inverted hull) ----
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                float3 n = normalize(v.normal);
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                float3 wn = normalize(mul((float3x3)unity_ObjectToWorld, n));
                world.xyz += wn * _OutlineWidth;
                o.pos = mul(UNITY_MATRIX_VP, world);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        // ---- toon lit pass ----
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Color, _ShadowTint, _RimColor;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
                float3 view : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.view = WorldSpaceViewDir(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float ndl = dot(n, _WorldSpaceLightPos0.xyz) * 0.5 + 0.5;
                // three hard-ish bands
                float band = ndl > 0.62 ? 1.0 : (ndl > 0.34 ? 0.7 : 0.45);
                fixed3 lit = lerp(_ShadowTint.rgb, fixed3(1,1,1), band);
                fixed3 albedo = _Color.rgb * tex2D(_MainTex, i.uv).rgb;
                fixed3 col = albedo * lit * _LightColor0.rgb
                           + albedo * UNITY_LIGHTMODEL_AMBIENT.rgb * 0.9;
                float rim = pow(1.0 - saturate(dot(normalize(i.view), n)), 3.0);
                col += _RimColor.rgb * rim;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
