Shader "Custom/BackgroundSmoke"
{
    Properties
    {
        _SmokeColor ("Smoke Color", Color) = (0.12, 0.10, 0.08, 1)
        _SmokeOpacity ("Smoke Opacity", Range(0, 1)) = 0.4
        _EdgeReach ("Edge Reach", Range(0.1, 1.5)) = 0.75
        _EdgeSoftness ("Edge Softness", Range(0.5, 4)) = 1.5
        _VignetteColor ("Vignette Color", Color) = (0, 0, 0, 1)
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.45
        _NoiseTex ("Noise Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _SmokeColor;
            float _SmokeOpacity;
            float _EdgeReach;
            float _EdgeSoftness;
            fixed4 _VignetteColor;
            float _VignetteStrength;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);
                float reach = max(0.01, _EdgeReach);

                float vignette = pow(saturate(dist / reach), _EdgeSoftness);
                float vignetteAlpha = vignette * _VignetteStrength;

                float noise = tex2D(_NoiseTex, i.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw).r;
                float edgeMask = pow(saturate(dist / reach), _EdgeSoftness * 0.7);
                float smokeAlpha = noise * edgeMask * _SmokeOpacity;

                fixed3 finalColor = _SmokeColor.rgb * smokeAlpha + _VignetteColor.rgb * vignetteAlpha * (1 - smokeAlpha);
                float finalAlpha = saturate(vignetteAlpha + smokeAlpha);

                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
}
