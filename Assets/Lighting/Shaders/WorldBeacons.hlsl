#ifndef WORLD_BEACONS_INCLUDED
#define WORLD_BEACONS_INCLUDED

// Story lights that guide the player and the park lamp posts (StepBeacon, up to 48): small point lights on the carousel, the generator, the Ferris wheel.
// Most of the scene is unlit, so real lights can't show on it: the light is computed here, like Zatan's eyes, and added
// after the radial fog. It fades with its own, much thinner fog, so a beacon reads from far away but not across the map.
// Ambient lights (StepBeacon with ambient on: the booths) have 8 slots of their own, given to the nearest ones.
// All zero when no beacon is lit: nothing changes. Works in CG and URP HLSL passes.
#define WORLD_BEACON_MAX 48
#define WORLD_AMBIENT_MAX 8
float _BeaconCount;
float _BeaconFogDensity;               // exponential fog of the beacon light, per metre from the camera
float4 _BeaconPosRange48[WORLD_BEACON_MAX];  // xyz: position, w: range
float4 _BeaconColor48[WORLD_BEACON_MAX];     // rgb: colour x intensity
float _AmbientLightCount;
float4 _AmbientLightPosRange[WORLD_AMBIENT_MAX];
float4 _AmbientLightColor[WORLD_AMBIENT_MAX];

// How much of a beacon-lit point the camera sees through the fog.
float BeaconSeen(float3 positionWS)
{
    return exp(-_BeaconFogDensity * distance(positionWS, _WorldSpaceCameraPos.xyz));
}

float3 BeaconPool(float4 posRange, float3 color, float3 positionWS, float3 normalWS)
{
    float3 toLight = posRange.xyz - positionWS;
    float dist = length(toLight);
    float3 lightDir = toLight / max(dist, 0.001);
    float reach = saturate(dist / max(posRange.w, 0.001));
    reach = 1.0 - reach * reach;
    reach *= reach;
    float facing = lerp(0.35, 1.0, saturate(dot(normalWS, lightDir)));
    return color * reach * facing;
}

float3 WorldBeacons(float3 albedo, float3 positionWS)
{
    // Face normal from the position, like ZatanEyeLight: the kit's meshes bake their shading. Taken before any early out (derivatives).
    float3 normalWS = normalize(cross(ddy(positionWS), ddx(positionWS)));
    float3 toCamera = _WorldSpaceCameraPos.xyz - positionWS;
    if (dot(normalWS, toCamera) < 0.0) normalWS = -normalWS;

    if (_BeaconCount < 0.5 && _AmbientLightCount < 0.5) return 0.0;

    float3 sum = 0.0;
    [loop] for (int i = 0; i < WORLD_BEACON_MAX; i++)
    {
        if (i >= (int)_BeaconCount) break;
        sum += BeaconPool(_BeaconPosRange48[i], _BeaconColor48[i].rgb, positionWS, normalWS);
    }
    [unroll] for (int j = 0; j < WORLD_AMBIENT_MAX; j++)
    {
        if (j >= (int)_AmbientLightCount) break;
        sum += BeaconPool(_AmbientLightPosRange[j], _AmbientLightColor[j].rgb, positionWS, normalWS);
    }
    // The minimum keeps the pool readable on the night floor, whose own colour is almost black.
    return (1.0 - exp(-max(albedo, 0.08) * sum)) * BeaconSeen(positionWS);
}

#endif
