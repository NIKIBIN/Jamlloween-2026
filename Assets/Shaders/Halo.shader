Shader "Circus/Halo"
{
    // Additive soft glow for zone beacons. Uses its own, much weaker fog so colored lights read
    // through the scene fog at 30-50 m.
    Properties
    {
        _Color ("Color", Color) = (1, 0.2, 0.9, 1)
        _Intensity ("Intensity", Float) = 1.5
        _FogScale ("Fog scale (x scene fog)", Float) = 0.35
        _Falloff ("Edge falloff", Float) = 3.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Back
        Pass
        {
            Name "Halo"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity, _FogScale, _Falloff;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; float d : TEXCOORD2; };

            V vert(A i)
            {
                V o;
                float3 w = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(w);
                o.n = TransformObjectToWorldNormal(i.n);
                o.v = GetWorldSpaceViewDir(w);
                o.d = length(_WorldSpaceCameraPos - w);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float f = saturate(dot(normalize(i.n), normalize(i.v)));
                f = pow(f, _Falloff);
                float fogD = unity_FogParams.x * _FogScale * i.d;   // exp2 density * scale
                float fog = exp2(-fogD * fogD);
                float nearFade = saturate((i.d - 3.0) / 10.0);   // up close it is just the lamp, not a glowing ball
                return half4(_Color.rgb * _Intensity * f * fog * nearFade, 1);
            }
            ENDHLSL
        }
    }
}
