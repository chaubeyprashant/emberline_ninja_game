// Mobile-lean physically-based surface shader for the realistic pass.
// Replaces Emberline/Toon: no cel banding, no inverted-hull ink outline.
// GGX specular + Lambert diffuse, real shadow receiving (the toon shader cast
// shadows via its fallback but never received them, which is why surfaces never
// darkened), additive point lights for practicals, and a wear/grime term that
// breaks up flat albedo without needing per-asset detail maps.
Shader "Emberline/Surface"
{
    Properties
    {
        _Color ("Albedo Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.25
        _Metallic ("Metallic", Range(0,1)) = 0
        _SpecTint ("Specular Tint", Color) = (1,1,1,1)
        _AmbientBoost ("Ambient Boost", Range(0,2)) = 1
        _RimColor ("Rim", Color) = (0.35,0.42,0.55,1)
        _RimPower ("Rim Power", Range(1,10)) = 4
        _RimStrength ("Rim Strength", Range(0,2)) = 0.25
        // Procedural grime: cheap triplanar-ish noise darkening in crevices and
        // downward faces. Gives leather/stone/metal believable dirt with no maps.
        _WearStrength ("Wear Strength", Range(0,1)) = 0.25
        _WearScale ("Wear Scale", Range(0.2,12)) = 3
        _WearColor ("Wear Colour", Color) = (0.22,0.20,0.18,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        fixed4 _Color, _SpecTint, _RimColor, _WearColor;
        sampler2D _MainTex;
        float4 _MainTex_ST;
        float _Smoothness, _Metallic, _AmbientBoost;
        float _RimPower, _RimStrength, _WearStrength, _WearScale;

        // Cheap value noise — three sine octaves in world space. Not a beauty
        // filter; it exists to stop large flat albedo blocks reading as plastic.
        float Grime(float3 wpos)
        {
            float3 p = wpos * _WearScale;
            float n = sin(p.x) * sin(p.y * 1.3) * sin(p.z * 0.7);
            n += 0.5 * sin(p.x * 2.1 + 1.7) * sin(p.z * 1.9);
            return saturate(n * 0.5 + 0.5);
        }

        // GGX/Trowbridge-Reitz normal distribution, the cheap half of a PBR spec.
        float SpecGGX(float3 n, float3 h, float rough)
        {
            float a = max(rough * rough, 0.002);
            float ndh = saturate(dot(n, h));
            float d = (ndh * ndh) * (a * a - 1.0) + 1.0;
            return (a * a) / (3.14159 * d * d);
        }
        ENDCG

        // ---------------- base pass: sun + ambient + shadows ----------------
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0

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
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4)
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.wpos);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 h = normalize(lightDir + viewDir);

                fixed3 albedo = _Color.rgb * tex2D(_MainTex, i.uv).rgb;

                // Grime settles on upward faces and in the noise's dark bands.
                float wear = Grime(i.wpos) * saturate(0.45 + 0.55 * n.y);
                albedo = lerp(albedo, _WearColor.rgb * albedo * 1.6,
                              wear * _WearStrength);

                float rough = 1.0 - _Smoothness;
                float ndl = saturate(dot(n, lightDir));
                UNITY_LIGHT_ATTENUATION(atten, i, i.wpos);

                // Metals take their specular colour from the albedo.
                float3 specColor = lerp(_SpecTint.rgb * 0.16, albedo, _Metallic);
                float3 diffColor = albedo * (1.0 - _Metallic);

                float spec = SpecGGX(n, h, rough) * ndl;
                float3 direct = (diffColor + specColor * spec) * _LightColor0.rgb * ndl * atten;

                // Hemispheric ambient: sky above, bounce below. Flat ambient is
                // what made the old look read as unlit plastic in shadow.
                float3 skyAmb = unity_AmbientSky.rgb;
                float3 gndAmb = unity_AmbientGround.rgb;
                float3 ambient = lerp(gndAmb, skyAmb, saturate(n.y * 0.5 + 0.5));
                ambient *= _AmbientBoost;

                float rim = pow(1.0 - saturate(dot(viewDir, n)), _RimPower);
                float3 col = direct + diffColor * ambient
                             + _RimColor.rgb * rim * _RimStrength * saturate(ndl + 0.35);

                fixed4 outc = fixed4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, outc);
                return outc;
            }
            ENDCG
        }

        // ---------------- additive pass: lanterns, torches, fire -------------
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd
            #pragma target 3.0

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
                UNITY_FOG_COORDS(5)
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.wpos);
                float3 ld = normalize(_WorldSpaceLightPos0.xyz - i.wpos * _WorldSpaceLightPos0.w);
                float3 h = normalize(ld + viewDir);
                UNITY_LIGHT_ATTENUATION(atten, i, i.wpos);

                fixed3 albedo = _Color.rgb * tex2D(_MainTex, i.uv).rgb;
                float rough = 1.0 - _Smoothness;
                float ndl = saturate(dot(n, ld));
                float3 specColor = lerp(_SpecTint.rgb * 0.16, albedo, _Metallic);
                float3 diffColor = albedo * (1.0 - _Metallic);
                float spec = SpecGGX(n, h, rough) * ndl;

                fixed4 outc = fixed4((diffColor + specColor * spec)
                                     * _LightColor0.rgb * ndl * atten, 0);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, outc, fixed4(0,0,0,0));
                return outc;
            }
            ENDCG
        }

        // Explicit caster so shadows do not depend on a fallback shader.
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma vertex vertS
            #pragma fragment fragS
            #pragma multi_compile_shadowcaster
            struct v2fS { V2F_SHADOW_CASTER; };
            v2fS vertS(appdata_base v)
            {
                v2fS o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            fixed4 fragS(v2fS i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
