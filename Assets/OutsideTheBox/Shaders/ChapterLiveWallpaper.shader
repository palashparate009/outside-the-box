Shader "OutsideTheBox/ChapterLiveWallpaper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorA ("Color A", Color) = (0.13, 0.55, 0.78, 1)
        _ColorB ("Color B", Color) = (0.48, 0.82, 0.92, 1)
        _Accent ("Accent", Color) = (0.76, 0.96, 1, 1)
        _Speed ("Speed", Range(0, 3)) = 0.55
        _Pattern ("Pattern", Range(0, 6)) = 0
        _Intensity ("Intensity", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent-100"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _ColorA;
            fixed4 _ColorB;
            fixed4 _Accent;
            half _Speed;
            half _Pattern;
            half _Intensity;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half hash21(half2 value)
            {
                value = frac(value * half2(123.34h, 456.21h));
                value += dot(value, value + 45.32h);
                return frac(value.x * value.y);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                half2 uv = input.uv;
                half2 centered = uv - 0.5h;
                half time = _Time.y * _Speed;

                // Slow domain-warped aurora bands replace the old high-contrast stripes.
                // Pattern selects a slightly different flow direction per chapter without
                // changing the calm visual language of the game.
                half variant = _Pattern * 0.37h;
                half2 flow = half2(
                    sin(uv.y * (3.2h + variant) + time * 0.52h),
                    cos(uv.x * (2.7h + variant * 0.6h) - time * 0.41h)) * 0.065h;
                half2 warped = uv + flow;
                half ribbonOne = 0.5h + 0.5h * sin(warped.x * (5.0h + variant) + warped.y * 2.2h + time * 0.7h);
                half ribbonTwo = 0.5h + 0.5h * sin(warped.y * (4.2h + variant * 0.5h) - warped.x * 1.7h - time * 0.48h);
                half aurora = smoothstep(0.56h, 0.96h, ribbonOne * 0.62h + ribbonTwo * 0.38h);

                half softWave = 0.5h + 0.5h * sin(warped.x * 2.7h + time * 0.28h);
                half gradient = saturate(warped.y * 0.74h + softWave * 0.26h);
                fixed3 baseColor = lerp(_ColorA.rgb, _ColorB.rgb, gradient);
                baseColor = lerp(baseColor, _Accent.rgb, aurora * _Intensity * 0.42h);

                // Two broad light pools continuously cross the screen. Squared distance
                // avoids extra square roots and keeps the effect inexpensive on phones.
                half2 lightOnePosition = half2(sin(time * 0.83h), cos(time * 0.61h)) * 0.34h;
                half2 lightTwoPosition = half2(cos(time * 0.47h), sin(time * 0.72h)) * 0.42h;
                half2 lightOneDelta = centered - lightOnePosition;
                half2 lightTwoDelta = centered - lightTwoPosition;
                half lightOne = 1.0h - smoothstep(0.015h, 0.24h, dot(lightOneDelta, lightOneDelta));
                half lightTwo = 1.0h - smoothstep(0.02h, 0.32h, dot(lightTwoDelta, lightTwoDelta));
                baseColor += _Accent.rgb * lightOne * _Intensity * 0.16h;
                baseColor = lerp(baseColor, _ColorA.rgb, lightTwo * _Intensity * 0.12h);
                half vignette = saturate(1.0h - dot(centered, centered) * 0.72h);
                baseColor *= lerp(0.86h, 1.0h, vignette);
                if (_Pattern > 5.5h)
                {
                    half stars = smoothstep(0.975h, 1.0h, hash21(floor((uv + time * 0.002h) * 54.0h)));
                    baseColor += _Accent.rgb * stars * 0.3h;
                }
                baseColor += (hash21(uv * 180.0h + time) - 0.5h) * 0.006h;
                return fixed4(saturate(baseColor), 1.0h) * input.color;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
