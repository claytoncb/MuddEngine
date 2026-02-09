vec3 midpoint(vec3 a, vec3 b)
{
    return (a + b) * 0.5;
}
bool simpleBlockerExists(vec3 sampleWorld, vec2 sampleScreen, int selfSpriteIndex)
{
    for (int i = 0; i < muddObjectCount; ++i)
    {
        if (i == selfSpriteIndex) continue;

        vec2 spriteBottomLeft = readVec2(u_SpriteData, i, 1);
        vec2 visibleSize      = readVec2(u_SpriteData, i, 6) * cameraZoom;
        vec2 visibleOffset    = readVec2(u_SpriteData, i, 5) * cameraZoom;

        vec2 minB = spriteBottomLeft + visibleOffset;
        vec2 maxB = minB + visibleSize;

        if (sampleScreen.x < minB.x || sampleScreen.x > maxB.x) continue;
        if (sampleScreen.y < minB.y || sampleScreen.y > maxB.y) continue;

        return true; // midpoint lies inside another sprite's quad
    }

    return false;
}
float computeShadowFactor_SingleBlocker(
    vec3 pixelWorldPos,
    vec3 normal,
    int spriteIndex
){
    if (lightCount <= 0)
        return 1.0;

    float shadowAccum = 1.0;

    for (int i = 0; i < lightCount; ++i)
    {
        vec3 L = lightPositions[i];

        // --- directional shadow (from 7.1)
        vec3 dir = normalize(L - pixelWorldPos);
        float NdotL = dot(normal, dir);
        float directionalShadow = 1.0 - clamp(NdotL, 0.0, 1.0);

        // --- distance falloff (from 7.0)
        float dist = length(L - pixelWorldPos);
        float radius = lightRadii[i];
        if (radius <= 0.0) radius = 300.0;
        float distanceShadow = clamp(dist / radius, 0.0, 1.0);

        // --- NEW: single-step blocker test
        vec3 midWorld = midpoint(pixelWorldPos, L);

        vec2 midScreen = (midWorld.xy - cameraOffset) * cameraZoom;

        float blockerShadow = simpleBlockerExists(midWorld, midScreen, spriteIndex)
            ? 1.0
            : 0.0;

        // Combine the three
        float combined = max(max(directionalShadow, distanceShadow), blockerShadow);

        shadowAccum *= combined;
    }

    return shadowAccum;
}