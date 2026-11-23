Shader "OutsideTheBox/Pipe Preview Lit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Skin", 2D) = "white" {}
        _UseTexture ("Use Skin", Float) = 0
        _BallProjection ("Ball Projection", Float) = 0
        _Gloss ("Gloss", Float) = 64
        _Unlit ("Unlit Capture", Float) = 0
        _BoardHole ("Board Hole", Vector) = (0.5,0.5,0.01,0.01)
        _CutBoardHole ("Cut Board Hole", Float) = 0
        _ClipPipe ("Reveal Pipe", Float) = 0
        _RevealY ("Pipe Reveal Limit", Float) = 10000
        _PipeRevealAxis ("Pipe Reveal Axis", Vector) = (0,1,0,0)
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 0
        [HideInInspector] _ZWrite ("Depth Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST, _Color;
            float _UseTexture, _BallProjection, _Gloss;
            float _Unlit, _CutBoardHole, _ClipPipe, _RevealY;
            float4 _BoardHole;
            float4 _PipeRevealAxis;
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Interpolated
            {
                float4 position : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float revealCoordinate : TEXCOORD3;
            };
            Interpolated vert(Input v)
            {
                Interpolated o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                float2 uv = lerp(v.uv, v.vertex.xy + 0.5, _BallProjection);
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                o.revealCoordinate = dot(v.vertex.xy, _PipeRevealAxis.xy);
                return o;
            }
            fixed4 frag(Interpolated i) : SV_Target
            {
                clip(lerp(1, _RevealY - i.revealCoordinate, _ClipPipe));
                float2 hole = (i.uv - _BoardHole.xy) / max(_BoardHole.zw, float2(0.0001,0.0001));
                clip(lerp(1, dot(hole,hole) - 1, _CutBoardHole));
                float3 normal = normalize(i.normal);
                float3 view = normalize(UnityWorldSpaceViewDir(i.world));
                float3 key = normalize(float3(-0.45, 0.7, -0.6));
                float3 fill = normalize(float3(0.8, -0.2, -0.5));
                fixed4 skin = tex2D(_MainTex, i.uv);
                float3 baseColor = _Color.rgb * lerp(float3(1,1,1), skin.rgb, lerp(skin.a, 1, _Unlit) * _UseTexture);
                float alpha = _Color.a * lerp(1, skin.a, _Unlit * _UseTexture);
                clip(alpha - 0.001);
                float diffuse = 0.24 + 0.6 * saturate(dot(normal, key)) + 0.2 * saturate(dot(normal, fill));
                float specular = pow(saturate(dot(normal, normalize(key + view))), max(1, _Gloss));
                float rim = pow(1 - saturate(dot(normal, view)), 3);
                float3 lit = baseColor * diffuse + specular * 0.7 + rim * float3(0.04,0.16,0.2);
                return fixed4(lerp(lit, baseColor, _Unlit), alpha);
            }
            ENDCG
        }
    }
}
