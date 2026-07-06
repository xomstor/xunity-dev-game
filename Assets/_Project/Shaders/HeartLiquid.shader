Shader "UI/HeartLiquid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Heart Mask", 2D) = "white" {}
        _Fill ("Fill Amount", Range(0,1)) = 1
        _ColorHigh ("Full HP Color", Color) = (1,0,0,1)
        _ColorMid ("Mid HP Color", Color) = (0,0,1,1)
        _ColorLow ("Empty HP Color", Color) = (0,0,0,1)
        _WaveSpeed ("Wave Speed", Float) = 2
        _WaveAmp ("Wave Amplitude", Float) = 0.03
        _BubbleSpeed ("Bubble Speed", Float) = 2
        _BubbleDensity ("Bubble Density", Float) = 25
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="true" }
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Fill;
            float4 _ColorHigh;
            float4 _ColorMid;
            float4 _ColorLow;
            float _WaveSpeed;
            float _WaveAmp;
            float _BubbleSpeed;
            float _BubbleDensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uv).a * i.color.a;
                if (mask <= 0.01)
                    return fixed4(0,0,0,0);

                float wave = sin(i.uv.x * 12 + _Time.y * _WaveSpeed) * _WaveAmp;
                float wave2 = cos(i.uv.x * 18 + _Time.y * _WaveSpeed * 1.3) * _WaveAmp * 0.5;
                float level = _Fill + wave + wave2;
                level = saturate(level);

                float inLiquid = step(i.uv.y, level);

                float4 liquidColor;
                if (_Fill > 0.5)
                    liquidColor = lerp(_ColorMid, _ColorHigh, saturate((_Fill - 0.5) * 2));
                else
                    liquidColor = lerp(_ColorLow, _ColorMid, saturate(_Fill * 2));

                // Bubbles
                float2 bubbleUv = i.uv * _BubbleDensity;
                bubbleUv.y += _Time.y * _BubbleSpeed;
                float2 cell = frac(bubbleUv) - 0.5;
                float2 id = floor(bubbleUv);
                float2 rnd = hash22(id);
                float bubble = 1.0 - smoothstep(0.05, 0.18, length(cell - (rnd - 0.5)));
                bubble *= step(0.7, rnd.x);
                bubble *= inLiquid;

                float4 bubbleColor = float4(1,1,1,0.6) * bubble;
                float4 finalColor = liquidColor * (0.85 + bubble * 0.3);
                finalColor.rgb += bubbleColor.rgb * bubble;

                return float4(finalColor.rgb, mask * inLiquid);
            }
            ENDCG
        }
    }
}
