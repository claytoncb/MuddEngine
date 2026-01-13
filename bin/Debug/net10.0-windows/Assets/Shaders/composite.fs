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

uniform vec3 lightPos[MAX_LIGHTS];     // raw sprite-space light positions
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
// Compute lighting contribution from one light
// ------------------------------------------------------------
vec3 ComputeLighting(vec3 pixelWorldPos, vec3 normal, vec3 lightWorldPos, vec3 lightColor)
{
    vec3 toLight = lightWorldPos - pixelWorldPos;
    float dist   = length(toLight);
    vec3 L       = toLight / dist;

    float NdotL = max(dot(normal, L), 0.0);
    float atten = 1.0 / (1.0 + dist * dist * 0.0005);

    return lightColor * NdotL * atten * 5.0;
}

// ------------------------------------------------------------
// MAIN
// ------------------------------------------------------------
void main()
{
    vec2 uv = gl_FragCoord.xy / screenSize;

    vec4 baseColor = texture(baseTex, uv);
    if (baseColor.a <= 0.001)
        discard;

    vec3 normal = normalize(texture(normalTex, uv).rgb * 2.0 - 1.0);

    vec3 pixelWorldPos = ReconstructPixelWorldPos(uv, gl_FragCoord.xy);

    vec3 totalLight = vec3(0.05);

    for (int i = 0; i < lightCount; i++)
    {
        totalLight += ComputeLighting(pixelWorldPos, normal, lightPos[i], lightColor[i]);
    }

    totalLight = clamp(totalLight, 0.0, 2.0);
    finalColor = vec4(baseColor.rgb * totalLight, baseColor.a);
}