// Cel-shaded character/prop shader with inverted-hull ink outline.
// Built-in render pipeline, mobile-cheap: one directional light, 3-band ramp, rim.
Shader "Emberline/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowTint ("Shadow Tint", Color) = (0.42,0.47,0.60,1)
        _RimColor ("Rim Color", Color) = (0.34,0.45,0.64,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2.6
        _RimStrength ("Rim Strength", Range(0,3)) = 0.8
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
                // Distance-scaled hull: a fixed world-space width reads as a hairline
                // up close and a fat black rind across the arena. Scaling by camera
                // distance keeps the ink roughly constant on screen instead.
                float dist = distance(_WorldSpaceCameraPos, world.xyz);
                world.xyz += wn * (_OutlineWidth * clamp(dist * 0.45, 0.75, 4.0));
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
            float _RimPower, _RimStrength;
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
                // Rim: wider and stronger than before, and biased toward the lit
                // side so characters catch the moon instead of glowing all round.
                float rim = pow(1.0 - saturate(dot(normalize(i.view), n)), _RimPower);
                float rimFacing = saturate(ndl * 1.4);
                col += _RimColor.rgb * rim * _RimStrength * (0.45 + 0.55 * rimFacing);
                return fixed4(col, 1);
            }
            ENDCG
        }

        // ---- additive pass: point lights (lanterns, torches) ----
        // Without this the toon materials only ever saw the single directional
        // light, so every lantern in the arena lit precisely nothing and the
        // scenes read flat and evenly grey.
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 wpos : TEXCOORD2;
                LIGHTING_COORDS(3,4)
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 ld = normalize(_WorldSpaceLightPos0.xyz
                                      - i.wpos * _WorldSpaceLightPos0.w);
                UNITY_LIGHT_ATTENUATION(atten, i, i.wpos);
                float ndl = saturate(dot(n, ld));
                // Banded like the base pass so added light stays cel-shaded.
                float band = ndl > 0.5 ? 1.0 : (ndl > 0.18 ? 0.55 : 0.0);
                fixed3 albedo = _Color.rgb * tex2D(_MainTex, i.uv).rgb;
                return fixed4(albedo * _LightColor0.rgb * band * atten, 0);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
