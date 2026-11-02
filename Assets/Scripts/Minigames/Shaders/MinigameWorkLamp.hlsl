#ifndef MINIGAME_WORK_LAMP_INCLUDED
#define MINIGAME_WORK_LAMP_INCLUDED

// The work lamp that lights a minigame, set globally by MinigameWorkLamp. PSX Unlit ignores real lights, so it gets the lamp
// the same way it gets the eyes of Zatan: computed here. Surfaces on Flashlight Fog Surface already react to the real spot light.
// All zero when no lamp is lit: nothing changes.
float4 _WorkLampPosition;  // xyz: origin, w: range
float4 _WorkLampDirection; // xyz: forward, w: cos of the outer angle
float4 _WorkLampColor;     // rgb: colour x intensity
float _WorkLampCosInner;

float3 MinigameWorkLamp(float3 albedo, float3 positionWS)
{
    // Face normal from the position, like ZatanEyeLight: the kit's meshes bake their shading. Taken before any early out (derivatives).
    float3 normalWS = normalize(cross(ddy(positionWS), ddx(positionWS)));
    float3 toCamera = _WorldSpaceCameraPos.xyz - positionWS;
    if (dot(normalWS, toCamera) < 0.0) normalWS = -normalWS;

    if (_WorkLampColor.r + _WorkLampColor.g + _WorkLampColor.b <= 0.0) return 0.0;

    float3 toLamp = _WorkLampPosition.xyz - positionWS;
    float dist = length(toLamp);
    float3 lightDir = toLamp / max(dist, 0.001);

    float cone = smoothstep(_WorkLampDirection.w, _WorkLampCosInner, dot(-lightDir, _WorkLampDirection.xyz));

    float reach = saturate(1.0 - (dist * dist) / (_WorkLampPosition.w * _WorkLampPosition.w));
    reach *= reach;

    float facing = lerp(0.35, 1.0, saturate(dot(normalWS, lightDir)));
    return albedo * _WorkLampColor.rgb * (cone * reach * facing / max(dist * dist, 0.3));
}

#endif
