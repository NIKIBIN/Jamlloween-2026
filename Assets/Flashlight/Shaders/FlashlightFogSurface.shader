Shader "Jamlloween/Flashlight Fog Surface"
{
    // Flat colour matched to PSX Unlit under the radial fog, plus additional lights (flashlight) and the light of Zatan's eyes.
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [Toggle] _IgnoreBlackout ("Stays Visible In Catch Blackout", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _IgnoreBlackout;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Zatan/Beam/ZatanEyeLight.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 lit = 0;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    lit += light.color * light.distanceAttenuation * saturate(dot(normalWS, light.direction));
                LIGHT_LOOP_END

                float3 color;
                if (_RadialFogEnabled > 0.5)
                {
                    color = _BaseColor.rgb * (FlashlightBrightness(input.positionWS) + lit);
                    color = FlashlightApplyFog(color, input.positionWS, unity_FogColor.rgb) + ZatanEyeLight(_BaseColor.rgb, input.positionWS);
                }
                else
                {
                    color = MixFog(_BaseColor.rgb * (1 + lit), input.fogFactor);
                }

                float3 blackout = _IgnoreBlackout > 0.5 ? ZatanScareLight(_BaseColor.rgb, input.positionWS, normalWS) : float3(0, 0, 0);
                return float4(lerp(color, blackout, _ZatanBlackout), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            float Frag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }
    }
}
