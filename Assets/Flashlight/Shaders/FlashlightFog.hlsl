#ifndef FLASHLIGHT_FOG_INCLUDED
#define FLASHLIGHT_FOG_INCLUDED

// Radial fog with a reveal cone. WorldLighting sets the base values, PlayerFlashlight the cone. Works in CG and URP HLSL passes.
// Outside the beam the fog is exponential: it starts to darken right at the player and fades out slowly, so there is no lit
// circle around the player with a hard dark edge. Inside the beam it is exp2: the light shows the near scene clearly.
// _RadialFogBaseDensity is an exp density; _RadialFogConeDensity is an exp2 density.
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

// How much of the scene the base fog leaves at this distance (exp), without the beam.
float RadialFogBaseSeen(float dist)
{
    return exp(-_RadialFogBaseDensity * dist);
}

float3 FlashlightApplyFog(float3 color, float3 positionWS, float3 fogColor)
{
    float cone = FlashlightConeAmount(positionWS);
    float dist = distance(positionWS, _FlashlightPosition.xyz);
    float baseSeen = RadialFogBaseSeen(dist);
    float coneDepth = _RadialFogConeDensity * dist;
    // The beam only clears the fog: it never shows less than the fog outside it.
    float seen = lerp(baseSeen, max(baseSeen, exp(-coneDepth * coneDepth)), cone);
    return lerp(fogColor, color, seen);
}

#endif
