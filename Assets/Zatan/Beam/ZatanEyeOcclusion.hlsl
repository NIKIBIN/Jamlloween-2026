#ifndef ZATAN_EYE_OCCLUSION_INCLUDED
#define ZATAN_EYE_OCCLUSION_INCLUDED

// Where Zatan's eye light is and what stands in its way, set globally by ZatanEyeLights. Shared by the light on the scenery
// (ZatanEyeLight.hlsl) and the visible beam (ZatanBeam.shader), so both end at the same obstacles.
float _ZatanEyeClipMargin;
float4 _ZatanEyePosition; // xyz: origin, w: range
float4 _ZatanEyeDirection; // xyz: forward, w: cos of the outer angle
float4 _ZatanEyeRight; // xyz: the cone's right axis, w: tangent of the outer half angle
float4 _ZatanEyeUp; // xyz: the cone's up axis

// Distance to the first obstacle along the axis (0) and two rings of ZATAN_EYE_RING_RAYS rays around it, at half the cone's
// radius and at its edge. Set by ZatanEyeLights.ProbeOcclusion: keep the counts and radii in step with it.
#define ZATAN_EYE_RING_RAYS 12
#define ZATAN_EYE_OCCLUSION_SAMPLES 25
float _ZatanEyeOcclusion[ZATAN_EYE_OCCLUSION_SAMPLES];

float ZatanEyeRingSample(int ring, float around)
{
    // around is already positive (0 .. 2 rings' worth): uint modulus is the cheap one.
    float cell = floor(around);
    float t = around - cell;
    uint a = (uint)cell % ZATAN_EYE_RING_RAYS;
    uint b = (a + 1u) % ZATAN_EYE_RING_RAYS;
    uint start = 1u + (uint)ring * ZATAN_EYE_RING_RAYS;
    return lerp(_ZatanEyeOcclusion[start + a], _ZatanEyeOcclusion[start + b], t);
}

// How far the light gets in this direction before an obstacle stops it, blended between the probed rays.
float ZatanEyeBlockerDistance(float3 direction)
{
    float forward = dot(direction, _ZatanEyeDirection.xyz);
    float x = dot(direction, _ZatanEyeRight.xyz);
    float y = dot(direction, _ZatanEyeUp.xyz);
    float radial = length(float2(x, y)) / max(forward, 0.001) / _ZatanEyeRight.w; // 0 on the axis, 1 at the edge
    float around = (atan2(y, x) / 6.2831853 + 1.0) * ZATAN_EYE_RING_RAYS;
    float inner = ZatanEyeRingSample(0, around);
    float outer = ZatanEyeRingSample(1, around);
    float centre = _ZatanEyeOcclusion[0];
    return radial < 0.5 ? lerp(centre, inner, radial * 2.0) : lerp(inner, outer, saturate(radial * 2.0 - 1.0));
}

// 1 in front of the obstacle in this point's direction, fading to 0 over the margin behind it. No shadow pass exists here.
float ZatanEyeUnblocked(float3 positionWS)
{
    float3 fromEye = positionWS - _ZatanEyePosition.xyz;
    float dist = length(fromEye);
    float blocker = ZatanEyeBlockerDistance(fromEye / max(dist, 0.001));
    return saturate((blocker + _ZatanEyeClipMargin - dist) / _ZatanEyeClipMargin);
}

#endif
