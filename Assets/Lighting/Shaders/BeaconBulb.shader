// A lit bulb in the PSX look: the texture times the baked vertex shading, glowing. It ignores the darkness of the night and
// the radial fog, and fades only with the thin beacon fog (WorldBeacons.hlsl), so the Ferris wheel and the little lamps read
// from far away. StepBeacon switches the renderers on and off.
Shader "Jamlloween/Beacon Bulb"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Glow ("Glow", Range(0, 8)) = 2
        _SnapRows ("Vertex Snap (rows on screen, 0 = off)", Float) = 240
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Assets/Zatan/Beam/ZatanEyeLight.hlsl"
            #include "Assets/Lighting/Shaders/WorldBeacons.hlsl"

            sampler2D _MainTex;
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Glow, _SnapRows;
            CBUFFER_END

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float3 positionWS : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                if (_SnapRows > 0)
                {
                    float2 grid = float2(_SnapRows * _ScreenParams.x / _ScreenParams.y, _SnapRows) * 0.5;
                    o.pos.xy = round(o.pos.xy / o.pos.w * grid) / grid * o.pos.w;
                }
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb = GammaToLinearSpace(v.color.rgb);
                #endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color * _Color;
                c.rgb = lerp(unity_FogColor.rgb, c.rgb * _Glow, BeaconSeen(i.positionWS));
                c.rgb *= 1.0 - _ZatanBlackout;
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
}
