#ifndef MINIGAME_WORK_LAMP_INCLUDED
#define MINIGAME_WORK_LAMP_INCLUDED

// The work lamps that light minigames, set globally by MinigameWorkLamp (up to two lit at once, one per slot). PSX Unlit
// ignores real lights, so it gets the lamps the same way it gets the eyes of Zatan: computed here. Surfaces on Flashlight
// Fog Surface already react to the real spot lights. A slot whose colour is zero is off: nothing changes.
float4 _WorkLampPosition;  // xyz: origin, w: range
float4 _WorkLampDirection; // xyz: forward, w: cos of the outer angle
float4 _WorkLampColor;     // rgb: colour x intensity
float _WorkLampCosInner;
float4 _WorkLamp2Position;
float4 _WorkLamp2Direction;
float4 _WorkLamp2Color;
float _WorkLamp2CosInner;

float3 MinigameWorkLampSlot(float3 albedo, float3 positionWS, float3 normalWS, float4 position, float4 direction, float4 color, float cosInner)
{
    if (color.r + color.g + color.b <= 0.0) return 0.0;

    float3 toLamp = position.xyz - positionWS;
    float dist = length(toLamp);
    float3 lightDir = toLamp / max(dist, 0.001);

    float cone = smoothstep(direction.w, cosInner, dot(-lightDir, direction.xyz));

    float reach = saturate(1.0 - (dist * dist) / (position.w * position.w));
    reach *= reach;

    float facing = lerp(0.35, 1.0, saturate(dot(normalWS, lightDir)));
    return albedo * color.rgb * (cone * reach * facing / max(dist * dist, 0.3));
}

float3 MinigameWorkLamp(float3 albedo, float3 positionWS)
{
    // Face normal from the position, like ZatanEyeLight: the kit's meshes bake their shading. Taken before any early out (derivatives).
    float3 normalWS = normalize(cross(ddy(positionWS), ddx(positionWS)));
    float3 toCamera = _WorldSpaceCameraPos.xyz - positionWS;
    if (dot(normalWS, toCamera) < 0.0) normalWS = -normalWS;

    return MinigameWorkLampSlot(albedo, positionWS, normalWS, _WorkLampPosition, _WorkLampDirection, _WorkLampColor, _WorkLampCosInner)
         + MinigameWorkLampSlot(albedo, positionWS, normalWS, _WorkLamp2Position, _WorkLamp2Direction, _WorkLamp2Color, _WorkLamp2CosInner);
}

#endif
