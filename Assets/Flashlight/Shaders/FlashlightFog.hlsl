#ifndef FLASHLIGHT_FOG_INCLUDED
#define FLASHLIGHT_FOG_INCLUDED

// Radial exp2 fog with a reveal cone. WorldLighting sets the base values, PlayerFlashlight the cone. Works in CG and URP HLSL passes.
// _RadialFogEnabled stays 0 in scenes without a PlayerFlashlight: their shaders keep the native fog untouched.
float _RadialFogEnabled;
float _RadialFogBaseDensity, _RadialFogConeDensity;
float _RadialFogBaseBrightness, _RadialFogConeBrightness;
float _FlashlightCosInner;
float4 _FlashlightPosition; // xyz: origin, w: 0..1 on amount
float4 _FlashlightDirection; // xyz: forward, w: cos of the outer angle

float FlashlightConeAmount(float3 positionWS)
{
    float3 toSurface = normalize(positionWS - _FlashlightPosition.xyz);
    float cosAngle = dot(toSurface, _FlashlightDirection.xyz);
    return smoothstep(_FlashlightDirection.w, _FlashlightCosInner, cosAngle) * _FlashlightPosition.w;
}

float FlashlightBrightness(float3 positionWS)
{
    // The beam only adds light: with a bright world profile it never darkens what is outside it.
    return lerp(_RadialFogBaseBrightness, max(_RadialFogBaseBrightness, _RadialFogConeBrightness), FlashlightConeAmount(positionWS));
}

float3 FlashlightApplyFog(float3 color, float3 positionWS, float3 fogColor)
{
    float density = lerp(_RadialFogBaseDensity, min(_RadialFogBaseDensity, _RadialFogConeDensity), FlashlightConeAmount(positionWS));
    float depth = density * distance(positionWS, _FlashlightPosition.xyz);
    return lerp(fogColor, color, exp(-depth * depth));
}

#endif
