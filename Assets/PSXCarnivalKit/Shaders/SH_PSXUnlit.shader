// The pack's look in Built-in and URP: one unlit pass (URP draws it as SRPDefaultUnlit), the texture times the baked vertex shading, with the PS1's wobbling vertices
// and swimming textures as sliders. The swim is off by default: Fab's review fails it as a visual defect seen close.
Shader "PSXCarnivalKit/PSX Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _SnapRows ("Vertex Snap (rows on screen, 0 = off)", Float) = 240
        _Affine ("Affine Texture Warp", Range(0, 1)) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0
        [Toggle] _IgnoreBlackout ("Stays Visible In Catch Blackout (Zatan)", Float) = 0
        _BeaconResponse ("Lamp Light Taken (1 = all, foliage less)", Range(0, 1)) = 1
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Assets/Zatan/Beam/ZatanEyeLight.hlsl"
            #include "Assets/Lighting/Shaders/WorldBeacons.hlsl"

            sampler2D _MainTex;
            CBUFFER_START(UnityPerMaterial) // for URP's SRP Batcher
            float4 _MainTex_ST;
            fixed4 _Color;
            float _SnapRows, _Affine, _Cutoff, _IgnoreBlackout, _BeaconResponse;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 affine : TEXCOORD1; // uv * w and w: their ratio interpolates linearly on screen
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(2)
                float3 positionWS : TEXCOORD3;
                float3 normalWS : TEXCOORD4;
            };

            // In the catch blackout the world goes black; with _IgnoreBlackout (Zatan) the surface keeps the scare light.
            float3 Blackout(float3 lit, float3 albedo, v2f i)
            {
                float3 scare = _IgnoreBlackout > 0.5 ? ZatanScareLight(albedo, i.positionWS, normalize(i.normalWS)) : float3(0, 0, 0);
                return lerp(lit, scare, _ZatanBlackout);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                if (_SnapRows > 0)
                {
                    float2 grid = float2(_SnapRows * _ScreenParams.x / _ScreenParams.y, _SnapRows) * 0.5;
                    o.pos.xy = round(o.pos.xy / o.pos.w * grid) / grid * o.pos.w;
                }
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.affine = float3(o.uv * o.pos.w, o.pos.w);
                o.color = v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb = GammaToLinearSpace(v.color.rgb); // the shading was baked in sRGB
                #endif
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = lerp(i.uv, i.affine.xy / i.affine.z, _Affine);
                fixed4 c = tex2D(_MainTex, uv) * i.color * _Color;
                clip(c.a - _Cutoff);
                float3 albedo = c.rgb;
                if (_RadialFogEnabled > 0.5)
                {
                    float3 eyeLight = ZatanEyeLight(c.rgb, i.positionWS) + MinigameWorkLamp(c.rgb, i.positionWS) + WorldBeacons(c.rgb, i.positionWS) * _BeaconResponse;
                    c.rgb *= FlashlightBrightness(i.positionWS);
                    c.rgb = FlashlightApplyFog(c.rgb, i.positionWS, unity_FogColor.rgb) + eyeLight;
                    c.rgb = Blackout(c.rgb, albedo, i);
                    return c;
                }
                UNITY_APPLY_FOG(i.fogCoord, c);
                c.rgb += MinigameWorkLamp(c.rgb, i.positionWS) + WorldBeacons(c.rgb, i.positionWS) * _BeaconResponse;
                c.rgb = Blackout(c.rgb, albedo, i);
                return c;
            }
            ENDCG
        }
    }
}
