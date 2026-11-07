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
float4 _BeaconColor48[WORLD_BEACON_MAX];     // rgb: colour x intensity, a: soft falloff in metres (0: compact pool)
float4 _BeaconSpot48[WORLD_BEACON_MAX];      // xyz: spot direction (zero: a point light, all around), w: cos of the outer half-angle
float _AmbientLightCount;
float4 _AmbientLightPosRange[WORLD_AMBIENT_MAX];
float4 _AmbientLightColor[WORLD_AMBIENT_MAX];

// How much of a beacon-lit point the camera sees through the fog.
float BeaconSeen(float3 positionWS)
{
    return exp(-_BeaconFogDensity * distance(positionWS, _WorldSpaceCameraPos.xyz));
}

// soft 0: a compact pool that ends at the range. soft > 0 (the lamp posts): the light drops like a real lamp,
// 1 / (1 + (dist / soft)^2), and a window brings the long tail to zero at the range, with no visible edge.
float3 BeaconPool(float4 posRange, float3 color, float soft, float3 positionWS, float3 normalWS)
{
    float3 toLight = posRange.xyz - positionWS;
    float dist = length(toLight);
    float3 lightDir = toLight / max(dist, 0.001);
    float x = saturate(dist / max(posRange.w, 0.001));
    float reach;
    if (soft > 0.0)
    {
        float window = 1.0 - x * x * x * x;
        float d = dist / soft;
        reach = window * window / (1.0 + d * d);
    }
    else
    {
        reach = 1.0 - x * x;
        reach *= reach;
    }
    // A soft facing term: the PSX meshes have flat faces, and a strong one made each face a different flat shade (plastic).
    float facing = lerp(0.5, 1.0, saturate(dot(normalWS, lightDir)));
    return color * reach * facing;
}

// A spot: the same pool inside a cone, with a soft edge. A zero direction is a point light (the default).
float BeaconCone(float4 spot, float3 positionWS, float3 lightPos)
{
    if (dot(spot.xyz, spot.xyz) < 0.25) return 1.0;
    float3 fromLight = normalize(positionWS - lightPos);
    return smoothstep(spot.w, min(1.0, spot.w + 0.18), dot(fromLight, spot.xyz));
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
        sum += BeaconPool(_BeaconPosRange48[i], _BeaconColor48[i].rgb, _BeaconColor48[i].a, positionWS, normalWS)
             * BeaconCone(_BeaconSpot48[i], positionWS, _BeaconPosRange48[i].xyz);
    }
    [unroll] for (int j = 0; j < WORLD_AMBIENT_MAX; j++)
    {
        if (j >= (int)_AmbientLightCount) break;
        sum += BeaconPool(_AmbientLightPosRange[j], _AmbientLightColor[j].rgb, 0.0, positionWS, normalWS);
    }
    // The light takes the surface colour: a strong light brings the surface up to its own colour, never to white, so the
    // texture keeps its contrast near a lamp. The minimum is on the brightness and keeps the hue: the night floor, almost
    // black, still shows the pool, and a dark green hedge stays green (a minimum on each channel made dark colours grey).
    float3 surface = max(albedo, 0.0005);
    float brightness = dot(surface, float3(0.2126, 0.7152, 0.0722));
    surface *= max(brightness, 0.06) / brightness;
    return surface * (1.0 - exp(-sum)) * BeaconSeen(positionWS);
}

#endif
