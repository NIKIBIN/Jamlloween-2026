#ifndef ZATAN_EYE_LIGHT_INCLUDED
#define ZATAN_EYE_LIGHT_INCLUDED

#include "Assets/Flashlight/Shaders/FlashlightFog.hlsl"
#include "Assets/Scripts/Minigames/Shaders/MinigameWorkLamp.hlsl"
#include "Assets/Zatan/Beam/ZatanEyeOcclusion.hlsl"

// The light of Zatan's eyes on the scenery, set globally by ZatanEyeLights. Most of the scene is unlit, so real spot lights can't
// show on it. Only evaluated under the radial fog: scenes without it never reach this code.
float _ZatanEyeOn;
float _ZatanEyeCosInner;
float _ZatanEyeFogCut;
float _ZatanEyeMinAlbedo;
float4 _ZatanEyeColor; // rgb: colour x intensity

// The catch sequence, set globally by ZatanCatchSequence. 0 in every other moment and scene: nothing changes.
float _ZatanBlackout;
float4 _ZatanScareLight; // rgb: colour x intensity of the light only Zatan receives while the world is black

float3 ZatanScareLight(float3 albedo, float3 positionWS, float3 normalWS)
{
    float facing = saturate(dot(normalWS, normalize(_WorldSpaceCameraPos.xyz - positionWS)));
    float rim = 1.0 - facing;
    return albedo * _ZatanScareLight.rgb * (0.35 + 0.65 * facing) + _ZatanScareLight.rgb * 0.04 * rim * rim * rim;
}

// Added after the fog, so a lit surface still shows beyond the circle the player can see.
float3 ZatanEyeLight(float3 albedo, float3 positionWS)
{
    if (_ZatanEyeOn < 0.5) return 0.0; // Vanished, or a scene without him: its other globals are unset

    float3 toEye = _ZatanEyePosition.xyz - positionWS;
    float dist = length(toEye);
    float3 lightDir = toEye / max(dist, 0.001);

    float cone = smoothstep(_ZatanEyeDirection.w, _ZatanEyeCosInner, dot(-lightDir, _ZatanEyeDirection.xyz));

    float reach = saturate(dist / _ZatanEyePosition.w);
    reach = 1.0 - reach * reach;
    reach *= reach;

    // Past the obstacle in this direction the light fades out over the margin.
    float blocked = ZatanEyeUnblocked(positionWS);

    // Face normal from the position: the kit's meshes bake their shading, and this stops the far side of a wall from lighting up.
    float3 normalWS = normalize(cross(ddy(positionWS), ddx(positionWS)));
    float3 toCamera = _WorldSpaceCameraPos.xyz - positionWS;
    if (dot(normalWS, toCamera) < 0.0) normalWS = -normalWS;
    float facing = saturate(dot(normalWS, lightDir));

    float seen = lerp(exp(-_RadialFogBaseDensity * length(toCamera)), 1.0, _ZatanEyeFogCut);   // the base fog: exp

    // The minimum keeps the pool readable on the night floor, whose own colour is almost black.
    float3 lit = 1.0 - exp(-max(albedo, _ZatanEyeMinAlbedo) * _ZatanEyeColor.rgb * cone * reach * blocked * facing);
    return lit * seen;
}

#endif
