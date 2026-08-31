// Translucent unlit-toon for shades, mirror clones, and Flicker after-images.
Shader "Emberline/Ghost"
{
    Properties
    {
        _Color ("Color", Color) = (0.5,0.6,0.7,0.5)
        _RimColor ("Rim Color", Color) = (0.6,0.8,0.9,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color, _RimColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
                float3 view : TEXCOORD1;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.view = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float rim = pow(1.0 - saturate(dot(normalize(i.view), n)), 2.0);
                fixed3 col = _Color.rgb + _RimColor.rgb * rim;
                return fixed4(col, _Color.a * (0.55 + 0.45 * rim));
            }
            ENDCG
        }
    }
}
