#version 330 core

#define MAX_LIGHTS 32

uniform sampler2D baseTex;
uniform sampler2D normalTex;
uniform sampler2D depthTex;

uniform vec2  screenSize;
uniform float maxY;
uniform float maxZ;

// Camera2D uniforms
uniform vec2  cameraTarget;
uniform vec2  cameraOffset;
uniform float cameraZoom;
uniform vec3  cameraPos;

// Lights
uniform vec3 lightPos[MAX_LIGHTS];
uniform vec3 lightColor[MAX_LIGHTS];
uniform int  lightCount;

out vec4 finalColor;

void main()
{
    vec2 uv = gl_FragCoord.xy / screenSize;

    vec4 baseColor = texture(baseTex, uv);
    if (baseColor.a <= 0.001)
        discard;

    vec3 raw = texture(normalTex, uv).rgb * 2.0 - 1.0;
    vec3 normal = normalize(vec3(
        raw.r,  // world X-ish
        raw.g,  // world Y-ish
        raw.b   // world Z-ish
    ));

    // Depth buffer → encoded Y/Z/parallax
    vec4 depthData    = texture(depthTex, uv);
    float parallax    = 1.0 - depthData.r;
    float spriteYNorm = depthData.g;
    float spriteZNorm = depthData.b;

    // Undo Camera2D transform first
    vec2 screen      = gl_FragCoord.xy;
    vec2 worldScreen = (screen - cameraOffset) / cameraZoom + cameraTarget;

    // World X from screen
    float worldX = worldScreen.x;

    // World Y from normalized value + camera
    float worldY = ((spriteYNorm - 0.5) * (2.0 * maxY)) + cameraPos.y;
    worldY += parallax * 128.0; // parallax offset across the sprite

    // Distinguish floor vs upright
    bool isFloor = spriteZNorm < 0.001;

    float worldZ;
    if (isFloor)
    {
        // Floor tiles: Z comes directly from zNorm (usually 0 → flat ground)
        worldZ = spriteZNorm * maxZ;
    }
    else
    {
        // Upright sprites: use your isometric-ish projection inversion:
        // screenY = -(0.5 * worldY) - 8 * worldZ
        // => worldZ = -(screenY + 0.5 * worldY) / 8
        worldZ = -(worldScreen.y + 0.5 * worldY) / 8.0;
    }

    vec3 pixelWorldPos = vec3(worldX, worldY, worldZ);

    // Ambient
    vec3 totalLight = vec3(0.25);

    for (int i = 0; i < lightCount; i++)
    {
        vec3 toLight = lightPos[i] - pixelWorldPos;
        float dist   = length(toLight);
        vec3 L       = toLight / dist;

        float diff = max(dot(normal, L), 0.0);
        if (diff <= 0.0)
            continue;

        float atten = 1.0 / (1.0 + dist * dist * 0.001);

        totalLight += lightColor[i] * diff * atten * 1.5;
    }

    totalLight = clamp(totalLight, 0.0, 2.0);
    finalColor = vec4(baseColor.rgb * totalLight, baseColor.a);
}