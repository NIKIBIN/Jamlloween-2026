#ifndef WORLD_BEACONS_INCLUDED
#define WORLD_BEACONS_INCLUDED

// Story lights that guide the player (StepBeacon): small point lights on the carousel, the generator, the Ferris wheel.
// Most of the scene is unlit, so real lights can't show on it: the light is computed here, like Zatan's eyes, and added
// after the radial fog. It fades with its own, much thinner fog, so a beacon reads from far away but not across the map.
// All zero when no beacon is lit: nothing changes. Works in CG and URP HLSL passes.
#define WORLD_BEACON_MAX 8
float _BeaconCount;
float _BeaconFogDensity;               // exponential fog of the beacon light, per metre from the camera
float4 _BeaconPosRange[WORLD_BEACON_MAX];  // xyz: position, w: range
float4 _BeaconColor[WORLD_BEACON_MAX];     // rgb: colour x intensity

// How much of a beacon-lit point the camera sees through the fog.
float BeaconSeen(float3 positionWS)
{
    return exp(-_BeaconFogDensity * distance(positionWS, _WorldSpaceCameraPos.xyz));
}

float3 WorldBeacons(float3 albedo, float3 positionWS)
{
    // Face normal from the position, like ZatanEyeLight: the kit's meshes bake their shading. Taken before any early out (derivatives).
    float3 normalWS = normalize(cross(ddy(positionWS), ddx(positionWS)));
    float3 toCamera = _WorldSpaceCameraPos.xyz - positionWS;
    if (dot(normalWS, toCamera) < 0.0) normalWS = -normalWS;

    if (_BeaconCount < 0.5) return 0.0;

    float3 sum = 0.0;
    [unroll] for (int i = 0; i < WORLD_BEACON_MAX; i++)
    {
        if (i >= (int)_BeaconCount) break;
        float3 toLight = _BeaconPosRange[i].xyz - positionWS;
        float dist = length(toLight);
        float3 lightDir = toLight / max(dist, 0.001);
        float reach = saturate(dist / max(_BeaconPosRange[i].w, 0.001));
        reach = 1.0 - reach * reach;
        reach *= reach;
        float facing = lerp(0.35, 1.0, saturate(dot(normalWS, lightDir)));
        sum += _BeaconColor[i].rgb * reach * facing;
    }
    // The minimum keeps the pool readable on the night floor, whose own colour is almost black.
    return (1.0 - exp(-max(albedo, 0.08) * sum)) * BeaconSeen(positionWS);
}

#endif
