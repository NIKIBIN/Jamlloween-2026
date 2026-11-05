// A lit bulb in the PSX look: the texture times the baked vertex shading, glowing. It ignores the darkness of the night and
// the radial fog, and fades only with the thin beacon fog (WorldBeacons.hlsl), so the Ferris wheel and the little lamps read
// from far away. StepBeacon sets _BulbLevel (the glow now; 0: a dead bulb, dark glass under the normal fog) and _BulbBroken
// (only some segments of bulbs take part, like a short circuit). Segments are slices of the angle around the ring axis.
Shader "Jamlloween/Beacon Bulb"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Glow ("Glow", Range(0, 8)) = 2
        _SnapRows ("Vertex Snap (rows on screen, 0 = off)", Float) = 240
        _BulbLevel ("Level (set by StepBeacon)", Range(0, 1)) = 1
        _BulbBroken ("Broken (set by StepBeacon)", Range(0, 1)) = 0
        _Segments ("Broken Segments around the ring", Float) = 24
        _SegmentAxis ("Ring Axis (0 X, 1 Y, 2 Z; set by StepBeacon)", Float) = 1
        _SegmentCenter ("Ring Centre, object space (set by StepBeacon)", Vector) = (0, 0, 0, 0)
        _DeadBrightness ("Dead Bulb Brightness", Range(0, 1)) = 0.12
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
            #include "Assets/Flashlight/Shaders/FlashlightFog.hlsl"

            sampler2D _MainTex;
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Glow, _SnapRows, _BulbLevel, _BulbBroken, _Segments, _DeadBrightness, _SegmentAxis;
            float4 _SegmentCenter;
            CBUFFER_END

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float3 positionWS : TEXCOORD1; nointerpolation float segment : TEXCOORD2; };

            float Hash(float n) { return frac(sin(n * 12.9898 + 1.7) * 43758.5453); }

            // Share of the glow for a bulb. StepBeacon sends the flicker in _BulbLevel; broken, about 40% of the segments
            // stay dead, and the rest take part with their own strength and an odd dropout, so the ring never lights evenly.
            float BulbOn(float segment)
            {
                if (_BulbBroken < 0.5) return 1.0;
                float h = Hash(segment);
                if (h < 0.4) return 0.0;
                float tick = floor(_Time.y * 15.0);
                if (Hash(tick + segment * 7.31) < 0.2) return 0.0;
                return lerp(0.5, 1.0, Hash(segment * 3.17 + 0.5));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 ring = v.vertex.xyz - _SegmentCenter.xyz;
                float2 plane = _SegmentAxis < 0.5 ? ring.yz : (_SegmentAxis < 1.5 ? ring.xz : ring.xy);
                o.segment = floor((atan2(plane.y, plane.x) / 6.2831853 + 0.5) * _Segments);
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
                float3 glow = lerp(unity_FogColor.rgb, c.rgb * _Glow, BeaconSeen(i.positionWS));
                // A dead bulb is dark glass: lit and fogged like the scenery around it.
                float3 dead = c.rgb * _DeadBrightness;
                if (_RadialFogEnabled > 0.5) dead = FlashlightApplyFog(dead * FlashlightBrightness(i.positionWS), i.positionWS, unity_FogColor.rgb);
                else dead = lerp(unity_FogColor.rgb, dead, saturate(RadialFogBaseSeen(distance(i.positionWS, _WorldSpaceCameraPos.xyz))));
                c.rgb = lerp(dead, glow, saturate(_BulbLevel * BulbOn(i.segment)));
                c.rgb *= 1.0 - _ZatanBlackout;
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
}
