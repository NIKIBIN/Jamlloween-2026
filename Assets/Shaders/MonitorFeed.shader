Shader "Hidden/Circus/MonitorFeed"
{
    Properties
    {
        _MainTex ("Feed", 2D) = "black" {}
        _FeedTime ("Time", Float) = 0
        _Noise ("Noise", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }
        ZWrite Off ZTest Always Cull Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _FeedTime, _Noise;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }

            float hash(float2 p) { p = frac(p * float2(443.897, 441.423)); p += dot(p, p.yx + 19.19); return frac((p.x + p.y) * p.x); }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float row = floor(uv.y * 192.0);
                uv.x += (hash(float2(row, floor(_FeedTime * 15.0))) - 0.5) * 0.004;
                float3 c = tex2D(_MainTex, uv).rgb;
                float g = dot(c, float3(0.299, 0.587, 0.114));
                g = saturate(g * 1.6 + 0.04);                       // cheap gain: security cams see in the dark-ish
                g += (hash(uv * 512.0 + _FeedTime) - 0.5) * _Noise;
                g *= 0.8 + 0.2 * sin(uv.y * 192.0 * 3.14159);
                float roll = frac(_FeedTime * 0.12);
                g += (1.0 - saturate(abs(uv.y - roll) / 0.02)) * 0.15;
                return fixed4(g, g, g, 1);
            }
            ENDCG
        }
    }
}
