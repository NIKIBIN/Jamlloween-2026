Shader "Zatan/Beam"
{
    Properties
    {
        [HDR] _BeamColor ("Color", Color) = (1, 1, 1, 1)
        _LengthFalloff ("Length Falloff", Float) = 1.6
        _EdgeSoftness ("Edge Softness", Float) = 2
        _FogResistance ("Fog Resistance", Range(0, 1)) = 0.6
        _InsideDim ("Brightness With Camera Inside", Range(0, 1)) = 0.1
        _SoftIntersection ("Soft Intersection (m)", Float) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/Zatan/Beam/ZatanEyeOcclusion.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BeamColor;
                float _LengthFalloff;
                float _EdgeSoftness;
                float _FogResistance;
                float _InsideDim;
                float _SoftIntersection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float along : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.along = input.uv.y;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 toCamera = _WorldSpaceCameraPos - input.positionWS;
                float cameraDistance = length(toCamera);
                float3 view = toCamera / cameraDistance;
                float3 axis = normalize(UNITY_MATRIX_M._m02_m12_m22);

                // Side-on the cone fades at its silhouette; seen along its axis it stays lit, so a beam pointing at you still reads.
                float side = pow(abs(dot(normalize(input.normalWS), view)), _EdgeSoftness);
                float front = pow(abs(dot(axis, view)), 4.0);
                float shape = saturate(side + front * 0.6);
                float length01 = pow(saturate(1.0 - input.along), _LengthFalloff);
                float nearCamera = saturate(cameraDistance * 0.5);
                float fog = lerp(ComputeFogIntensity(input.fogFactor), 1.0, _FogResistance);

                // Inside the cone (radius = z in object space) every pixel would stack beam: dim it so it never washes the screen.
                float3 cameraOS = TransformWorldToObject(_WorldSpaceCameraPos);
                float insideDepth = (cameraOS.z > 0.0 && cameraOS.z < 1.0) ? saturate((length(cameraOS.xy) / max(cameraOS.z, 0.001) - 0.7) / 0.3) : 1.0;
                float inside = lerp(_InsideDim, 1.0, insideDepth);

                // Fade where the cone meets the world, so it never ends in a hard line on a wall or on the floor.
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float beamDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float soft = saturate((sceneDepth - beamDepth) / max(_SoftIntersection, 0.001));

                // The cone reaches as far as its longest clear ray; each part of it ends softly behind what blocks that direction,
                // so a pole or a corner at the edge doesn't cut the whole beam short.
                float unblocked = ZatanEyeUnblocked(input.positionWS);

                return float4(_BeamColor.rgb * shape * length01 * nearCamera * fog * inside * soft * unblocked, 1.0);
            }
            ENDHLSL
        }
    }
}
