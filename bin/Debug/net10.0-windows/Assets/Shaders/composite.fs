#version 330 core

#define MAX_LIGHTS 32

uniform sampler2D baseTex;
uniform sampler2D normalTex;
uniform sampler2D depthTex;

uniform vec2  screenSize;
uniform float maxY;
uniform float maxZ;

uniform vec2  cameraTarget;
uniform vec2  cameraOffset;
uniform float cameraZoom;
uniform vec3  cameraPos;

uniform vec3 lightPos[MAX_LIGHTS];
uniform vec3 lightColor[MAX_LIGHTS];
uniform int  lightCount;

out vec4 finalColor;

// ------------------------------------------------------------
// Convert screen pixel to world X/Y using Camera2D inverse
// ------------------------------------------------------------
vec2 WorldFromScreen(vec2 screen)
{
    return (screen - cameraOffset) / cameraZoom + cameraTarget;
}

// ------------------------------------------------------------
// Convert world X/Y back to screen coordinates
// ------------------------------------------------------------
vec2 ScreenFromWorld(vec2 worldXY)
{
    return (worldXY - cameraTarget) * cameraZoom + cameraOffset;
}

// ------------------------------------------------------------
// Encode world XY for depth-pass convention (Y flipped)
// ------------------------------------------------------------
vec2 EncodeDepthXY(vec3 worldPos)
{
    return vec2(worldPos.x, -worldPos.y);
}

// ------------------------------------------------------------
// Reconstruct per-pixel world position from depth buffer
// ------------------------------------------------------------
vec3 ReconstructPixelWorldPos(vec2 uv, vec2 screen)
{
    vec4 depthData = texture(depthTex, uv);

    float parallax    = 1.0 - depthData.r;
    float spriteYNorm = depthData.g;
    float spriteZNorm = depthData.b;

    vec2 worldScreen = WorldFromScreen(screen);

    float worldX = worldScreen.x;

    float spriteY = ((spriteYNorm - 0.5) * (2.0 * maxY)) + cameraPos.y;
    float worldY  = spriteY + parallax * 128.0;

    float worldZ = spriteZNorm * maxZ;

    return vec3(worldX, worldY, worldZ);
}

// ------------------------------------------------------------
// Compute lighting contribution from one light (no shadow)
// ------------------------------------------------------------
vec3 ComputeLighting(vec3 pixelWorldPos, vec3 normal, vec3 lightWorldPos, vec3 lightColor)
{
    vec3 toLight = lightWorldPos - pixelWorldPos;
    float dist   = length(toLight);
    vec3 L       = toLight / max(dist, 0.0001);

    float NdotL = max(dot(normal, L), 0.0);
    float atten = 1.0 / (1.0 + dist * dist * 0.0005);

    return lightColor * NdotL * atten * 5.0;
}

// ------------------------------------------------------------
// Screen-space ray traced shadow from pixel to light
// Returns 0.0 = no shadow, 1.0 = fully shadowed
// ------------------------------------------------------------
float TraceShadow(vec3 pixelWorldPos, vec3 lightWorldPos)
{
    // If pixel is above the light, it cannot be shadowed
    if (pixelWorldPos.z >= lightWorldPos.z)
        return 0.0;

    vec3 toLight = lightWorldPos - pixelWorldPos;
    float dist   = length(toLight);
    vec3 dir     = toLight / max(dist, 0.0001);

    // If ray is nearly horizontal, heightfield cannot occlude it
    if (abs(dir.z) < 0.0001)
        return 0.0;

    const int   STEPS   = 16;
    const float BIAS    = 1.0;
    const float START_T = 2.0;

    for (int i = 0; i < STEPS; i++)
    {
        float t = START_T + (float(i) / float(STEPS)) * (dist - START_T);
        if (t >= dist)
            break;

        vec3 sampleWorldPos = pixelWorldPos + dir * t;

        // Project using depth-pass encoding
        vec2 sampleScreen = ScreenFromWorld(EncodeDepthXY(sampleWorldPos));
        vec2 sampleUV     = sampleScreen / screenSize;

        if (sampleUV.x < 0.0 || sampleUV.x > 1.0 ||
            sampleUV.y < 0.0 || sampleUV.y > 1.0)
        {
            continue;
        }

        vec3 depthWorldPos = ReconstructPixelWorldPos(sampleUV, sampleScreen);

        // Occluder must be strictly in front of the ray sample,
        // but still below the light height.
        if (depthWorldPos.z >= sampleWorldPos.z + BIAS &&
            depthWorldPos.z <= lightWorldPos.z - BIAS)
        {
            return 1.0;
        }
    }

    return 0.0;
}

// ------------------------------------------------------------
// MAIN
// ------------------------------------------------------------
void main()
{
    vec2 screen = gl_FragCoord.xy;
    vec2 uv     = screen / screenSize;

    vec4 baseColor = texture(baseTex, uv);
    if (baseColor.a <= 0.001)
        discard;

    vec3 normal = normalize(texture(normalTex, uv).rgb * 2.0 - 1.0);
    vec3 pixelWorldPos = ReconstructPixelWorldPos(uv, screen);

    vec3 totalLight = vec3(0.05);

    for (int i = 0; i < lightCount; i++)
    {
        vec3  lightWorldPos = lightPos[i];
        float shadow        = TraceShadow(pixelWorldPos, lightWorldPos);
        float shadowFactor  = 1.0 - shadow;

        vec3 directLight = ComputeLighting(pixelWorldPos, normal, lightWorldPos, lightColor[i]);
        totalLight += directLight * shadowFactor;
    }

    totalLight = clamp(totalLight, 0.0, 2.0);
    finalColor = vec4(baseColor.rgb * totalLight, baseColor.a);
}