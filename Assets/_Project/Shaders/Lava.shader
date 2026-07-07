Shader "Custom/Lava"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask", 2D) = "white" {}
        _ColorDeep ("Deep Lava Color", Color) = (0.5, 0.0, 0.0, 1)
        _ColorHot ("Hot Lava Color", Color) = (1.0, 0.25, 0.0, 1)
        _ColorGlow ("Glow Edge Color", Color) = (1.0, 0.8, 0.1, 1)
        _FlowSpeed ("Flow Speed", Float) = 0.5
        _FlowScale ("Flow Scale", Float) = 4.0
        _WaveAmp ("Wave Amplitude", Float) = 0.04
        _WaveSpeed ("Wave Speed", Float) = 1.5
        _BubbleSpeed ("Bubble Speed", Float) = 1.2
        _BubbleDensity ("Bubble Density", Float) = 15
        _BubbleColor ("Bubble Color", Color) = (1, 0.9, 0.4, 1)
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
            float4 _ColorDeep;
            float4 _ColorHot;
            float4 _ColorGlow;
            float _FlowSpeed;
            float _FlowScale;
            float _WaveAmp;
            float _WaveSpeed;
            float _BubbleSpeed;
            float _BubbleDensity;
            float4 _BubbleColor;

            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash22(i).x;
                float b = hash22(i + float2(1, 0)).x;
                float c = hash22(i + float2(0, 1)).x;
                float d = hash22(i + float2(1, 1)).x;
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uv).a * i.color.a;
                if (mask <= 0.01)
                    return fixed4(0,0,0,0);

                float t = _Time.y;
                float2 flowUv = i.uv * _FlowScale;
                flowUv.y += t * _FlowSpeed;
                flowUv.x += t * _FlowSpeed * 0.3;

                float flow = noise(flowUv) * 0.5 + noise(flowUv * 2.0 + t * 0.5) * 0.25;
                flow = saturate(flow);

                float wave = sin(i.uv.x * 14 + t * _WaveSpeed) * _WaveAmp;
                wave += cos(i.uv.x * 22 + t * _WaveSpeed * 1.4) * _WaveAmp * 0.6;

                float edge = 1.0 - smoothstep(0.0, 0.25, i.uv.y);
                float surface = smoothstep(0.0, 0.35 + wave, i.uv.y + wave);

                float4 lavaColor = lerp(_ColorHot, _ColorDeep, surface * 0.8 + flow * 0.2);
                lavaColor = lerp(lavaColor, _ColorGlow, edge * 0.5 + flow * 0.3);

                // Bubbles
                float2 bubbleUv = i.uv * _BubbleDensity;
                bubbleUv.y += t * _BubbleSpeed;
                bubbleUv.x += sin(t * 0.7 + i.uv.y * 6) * 0.1;
                float2 cell = frac(bubbleUv) - 0.5;
                float2 id = floor(bubbleUv);
                float2 rnd = hash22(id);
                float bubble = 1.0 - smoothstep(0.04, 0.14, length(cell - (rnd - 0.5)));
                bubble *= step(0.65, rnd.x);
                bubble *= smoothstep(0.0, 0.5, i.uv.y + wave);
                bubble *= smoothstep(1.0, 0.6, i.uv.y);

                float4 finalColor = lavaColor * (1.0 + bubble * 0.5);
                finalColor.rgb += _BubbleColor.rgb * bubble * 0.8;
                finalColor.a = mask;

                return finalColor;
            }
            ENDCG
        }
    }
}
