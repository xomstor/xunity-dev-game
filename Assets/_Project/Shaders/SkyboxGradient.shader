Shader "Custom/SkyboxGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.3, 0.5, 0.8, 1)
        _HorizonColor ("Horizon Color", Color) = (0.9, 0.7, 0.5, 1)
        _BottomColor ("Bottom Color", Color) = (0.15, 0.1, 0.2, 1)
        _HorizonBlend ("Horizon Blend", Range(0.01, 1)) = 0.3
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 0.15)
        _CloudSpeed ("Cloud Speed", Range(0, 10)) = 1.0
        _CloudScale ("Cloud Scale", Range(1, 20)) = 5
        _CloudDensity ("Cloud Density", Range(0, 1)) = 0.3
        _SunColor ("Sun Glow Color", Color) = (1, 0.85, 0.6, 1)
        _SunIntensity ("Sun Glow Intensity", Range(0, 3)) = 1.0
        _SunAngle ("Sun Angle (degrees)", Range(0, 360)) = 120
        _SunSize ("Sun Glow Size", Range(0.05, 1)) = 0.3
        _ColorShift ("Color Shift Speed", Range(0, 1)) = 0.1
        _CloudLayer2Speed ("Cloud Layer 2 Speed", Range(0, 10)) = 0.7
        _CloudLayer2Scale ("Cloud Layer 2 Scale", Range(1, 30)) = 10
        _CloudLayer2Density ("Cloud Layer 2 Density", Range(0, 1)) = 0.15
        _CloudOffset1 ("Cloud Offset 1", Float) = 0
        _CloudOffset2 ("Cloud Offset 2", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" }
        Cull Off ZWrite Off Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            float4 _TopColor;
            float4 _HorizonColor;
            float4 _BottomColor;
            float _HorizonBlend;
            float4 _CloudColor;
            float _CloudSpeed;
            float _CloudScale;
            float _CloudDensity;
            float4 _SunColor;
            float _SunIntensity;
            float _SunAngle;
            float _SunSize;
            float _ColorShift;
            float _CloudLayer2Speed;
            float _CloudLayer2Scale;
            float _CloudLayer2Density;
            float _CloudOffset1;
            float _CloudOffset2;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.texcoord;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * noise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            float3 hueShift(float3 col, float shift)
            {
                float cosA = cos(shift);
                float sinA = sin(shift);
                float3x3 mat = float3x3(
                    0.299 + 0.701 * cosA + 0.168 * sinA,
                    0.587 - 0.587 * cosA + 0.330 * sinA,
                    0.114 - 0.114 * cosA - 0.497 * sinA,
                    0.299 - 0.299 * cosA - 0.328 * sinA,
                    0.587 + 0.413 * cosA + 0.035 * sinA,
                    0.114 - 0.114 * cosA + 0.292 * sinA,
                    0.299 - 0.300 * cosA + 1.250 * sinA,
                    0.587 - 0.588 * cosA - 1.050 * sinA,
                    0.114 + 0.886 * cosA - 0.203 * sinA
                );
                return mul(mat, col);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float h = dir.y;
                float t = _Time.y;

                // Slow color shift for subtle living-sky effect
                float shift = sin(t * _ColorShift) * 0.03;
                float3 topC = hueShift(_TopColor.rgb, shift);
                float3 horC = hueShift(_HorizonColor.rgb, shift * 0.5);

                float topBlend = smoothstep(0.0, _HorizonBlend, h);
                float bottomBlend = smoothstep(0.0, -_HorizonBlend, -h);

                fixed4 col = fixed4(lerp(horC, topC, topBlend), 1);
                col.rgb = lerp(col.rgb, _BottomColor.rgb, bottomBlend);

                // Sun glow — moves slowly across the sky
                float sunRad = radians(_SunAngle + t * 0.5);
                float3 sunDir = normalize(float3(cos(sunRad), sin(sunRad) * 0.5 + 0.15, 0.3));
                float sunDot = max(0, dot(dir, sunDir));
                float sunGlow = pow(sunDot, 1.0 / _SunSize) * _SunIntensity;
                sunGlow *= smoothstep(-0.1, 0.4, h);
                col.rgb += _SunColor.rgb * sunGlow * _SunColor.a;

                // Cloud layer 1 — large, slow
                float2 cloudUV1 = dir.xz / max(abs(dir.y), 0.1) * _CloudScale;
                cloudUV1 += float2(t * _CloudSpeed + _CloudOffset1, t * _CloudSpeed * 0.6 + _CloudOffset1);
                float clouds1 = fbm(cloudUV1);
                clouds1 = smoothstep(0.5, 0.9, clouds1) * _CloudDensity;
                clouds1 *= smoothstep(0.0, 0.3, h);
                col.rgb = lerp(col.rgb, _CloudColor.rgb, clouds1 * _CloudColor.a);

                // Cloud layer 2 — smaller, faster, different direction
                float2 cloudUV2 = dir.xz / max(abs(dir.y), 0.1) * _CloudLayer2Scale;
                cloudUV2 += float2(t * _CloudLayer2Speed * 1.3 + _CloudOffset2, t * _CloudLayer2Speed + _CloudOffset2);
                float clouds2 = fbm(cloudUV2);
                clouds2 = smoothstep(0.55, 0.85, clouds2) * _CloudLayer2Density;
                clouds2 *= smoothstep(0.05, 0.35, h);
                col.rgb = lerp(col.rgb, _CloudColor.rgb * 0.85, clouds2 * _CloudColor.a);

                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
