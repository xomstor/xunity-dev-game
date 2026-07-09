Shader "Custom/TeleportTunnel"
{
    Properties
    {
        _MainTex ("Starfield", 2D) = "white" {}
        _ColorA ("Center Color", Color) = (0.0, 1.0, 0.95, 1)
        _ColorB ("Edge Color", Color) = (1.0, 0.2, 0.8, 1)
        _Speed ("Speed", Float) = 4.0
        _Warp ("Warp", Float) = 5.0
        _Opacity ("Opacity", Range(0, 1)) = 1.0
        _PixelSize ("Pixel Size", Float) = 24.0
        _Glow ("Center Glow", Range(0, 4)) = 2.0
        _Chromatic ("Chromatic", Range(0, 1)) = 0.25
        _Layers ("Layers", Range(1, 5)) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
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
            float4 _MainTex_ST;
            fixed4 _ColorA;
            fixed4 _ColorB;
            float _Speed;
            float _Warp;
            float _Opacity;
            float _PixelSize;
            float _Glow;
            float _Chromatic;
            float _Layers;
            float _TimeY;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                p = floor(p);
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float sampleLayer(float angle, float radius, float layer)
            {
                float speedMul = 1.0 + layer * 0.7;
                float warpMul = 1.0 + layer * 0.6;
                float angleMul = 1.0 + layer * 0.35;
                float offset = layer * 0.37;

                float2 uvLayer;
                uvLayer.x = frac(angle * angleMul + offset);
                uvLayer.y = frac(-_TimeY * _Speed * speedMul + radius * _Warp * warpMul + offset);

                float2 pixelUV = floor(uvLayer * _PixelSize) / _PixelSize;
                float star = tex2D(_MainTex, pixelUV).r;

                float streak = pow(sin(frac(radius * (8.0 + layer * 4.0) - _TimeY * _Speed * (2.0 + layer)) * 3.14159), 2.0);
                streak *= 1.0 - smoothstep(0.0, 0.7, radius);

                return (star + streak * 0.35) * (1.0 - layer * 0.15);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - float2(0.5, 0.5);
                float r = length(uv);
                float a = atan2(uv.y, uv.x);

                float glow = 1.0 - smoothstep(0.0, 0.7, r);
                glow = pow(glow, 0.5) * _Glow;
                float vignette = 1.0 - smoothstep(0.4, 0.85, r);

                float chroma = _Chromatic * (1.0 - r) * 0.03;

                int layers = max(1, (int)_Layers);
                float3 rgb = float3(0, 0, 0);
                for (int l = 0; l < layers; l++)
                {
                    float layerFloat = (float)l;
                    float rSample = sampleLayer(a + chroma, r, layerFloat);
                    float gSample = sampleLayer(a, r, layerFloat);
                    float bSample = sampleLayer(a - chroma, r, layerFloat);
                    rgb += float3(rSample, gSample, bSample);
                }

                fixed4 tint = lerp(_ColorA, _ColorB, r);
                fixed4 col = tint * fixed4(rgb, 1.0) * glow * vignette;
                col.a *= _Opacity * i.color.a;
                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
