Shader "Custom/BackgroundEdgeBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeOpacity ("Edge Opacity", Range(0, 1)) = 0.35
        _EdgeWidth ("Edge Width", Range(0.001, 0.5)) = 0.12
        _NoiseScale ("Noise Scale", Range(1, 80)) = 24
        _PixelSize ("Pixel Size", Range(1, 64)) = 12
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _EdgeOpacity;
            float _EdgeWidth;
            float _NoiseScale;
            float _PixelSize;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float hash21(float2 p)
            {
                p = floor(p);
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                float distToEdge = min(min(i.uv.x, 1.0 - i.uv.x), min(i.uv.y, 1.0 - i.uv.y));
                float edge = 1.0 - smoothstep(0.0, _EdgeWidth, distToEdge);

                float2 blockUv = floor(i.uv * _PixelSize) / _PixelSize;
                float noise = hash21(blockUv * _NoiseScale);
                float smoke = saturate(edge * lerp(0.55, 1.15, noise) * _EdgeOpacity);

                col.rgb = lerp(col.rgb, float3(0, 0, 0), smoke);
                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
