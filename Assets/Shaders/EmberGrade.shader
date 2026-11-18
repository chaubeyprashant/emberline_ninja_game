// Cinematic grade for the Built-in pipeline: filmic tonemap, saturation pull,
// lift/gain, a soft vignette, and (pass 0) the bloom built by passes 1-2 at
// quarter resolution and below. No depth reads, so it stays affordable on mobile.
Shader "Emberline/Grade"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Saturation ("Saturation", Range(0,2)) = 0.82
        _Contrast ("Contrast", Range(0.5,2)) = 1.08
        _Lift ("Shadow Lift", Color) = (0.03,0.04,0.06,0)
        _Gain ("Highlight Gain", Color) = (1.02,1.0,0.96,0)
        _Vignette ("Vignette", Range(0,1)) = 0.34
        _Exposure ("Exposure", Range(0.2,3)) = 1.05
        _BloomTex ("Bloom", 2D) = "black" {}
        _BloomIntensity ("Bloom Intensity", Range(0,3)) = 0
        _Threshold ("Bloom Threshold", Range(0,1)) = 0.7
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _BloomTex;
            float _Saturation, _Contrast, _Vignette, _Exposure, _BloomIntensity;
            fixed4 _Lift, _Gain;

            // ACES-ish filmic curve. Cheap approximation — the point is to roll
            // highlights off instead of clipping them, which is most of what
            // separates "rendered" from "photographed".
            float3 Tonemap(float3 x)
            {
                const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }

            fixed4 frag (v2f_img i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb * _Exposure;
                // Lantern windows, fire and lit shoji bleed past their edges.
                col += tex2D(_BloomTex, i.uv).rgb * _BloomIntensity;
                col = Tonemap(col);

                // Lift/gain before saturation so the tint lands on the whole ramp.
                col = col * _Gain.rgb + _Lift.rgb * (1.0 - col);

                float luma = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(float3(luma, luma, luma), col, _Saturation);
                col = saturate((col - 0.5) * _Contrast + 0.5);

                // Vignette: distance from centre, eased. Keeps the eye centred on
                // the fight without a texture lookup.
                float2 d = i.uv - 0.5;
                float vig = 1.0 - _Vignette * saturate(dot(d, d) * 2.2);
                col *= vig;

                return fixed4(col, 1);
            }
            ENDCG
        }

        // Pass 1: bright-pass and 4-tap downsample to quarter resolution.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Threshold;
            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy;
                float3 c = (tex2D(_MainTex, i.uv + float2(-o.x, -o.y)).rgb + tex2D(_MainTex, i.uv + float2(o.x, -o.y)).rgb
                          + tex2D(_MainTex, i.uv + float2(-o.x, o.y)).rgb + tex2D(_MainTex, i.uv + float2(o.x, o.y)).rgb) * 0.25;
                // Soft knee: only what is already near white contributes.
                float br = max(c.r, max(c.g, c.b));
                float k = saturate((br - _Threshold) / max(1.0 - _Threshold, 1e-4));
                return fixed4(c * k * k, 1);
            }
            ENDCG
        }

        // Pass 2: 4-tap diagonal blur, used for each step down and back up.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * 1.5;
                float3 c = tex2D(_MainTex, i.uv + float2(-o.x, -o.y)).rgb + tex2D(_MainTex, i.uv + float2(o.x, -o.y)).rgb
                         + tex2D(_MainTex, i.uv + float2(-o.x, o.y)).rgb + tex2D(_MainTex, i.uv + float2(o.x, o.y)).rgb;
                return fixed4(c * 0.25, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
