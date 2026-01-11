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

vec3 DecodeNormal(vec2 uv)
{
    vec3 raw = texture(normalTex, uv).rgb;
    return normalize(raw * 2.0 - 1.0);
}

vec2 ScreenToWorldScreen(vec2 screen)
{
    return (screen - cameraOffset) / cameraZoom + cameraTarget;
}

vec3 DepthToWorld(vec4 depthData, vec2 worldScreen)
{
    float parallax    = 1.0 - depthData.r;
    float spriteYNorm = depthData.g;
    float spriteZNorm = depthData.b;

    float worldX = worldScreen.x;

    float spriteY = ((spriteYNorm - 0.5) * (2.0 * maxY)) + cameraPos.y;
    float worldY  = spriteY + parallax * 128.0;

    bool isFloor = spriteZNorm < 0.001;

    float worldZ;
    if (isFloor)
    {
        worldZ = spriteZNorm * maxZ;
    }
    else
    {
        float screenY = worldScreen.y;
        worldZ = -(screenY + 0.5 * worldY) / 8.0;
    }

    return vec3(worldX, worldY, worldZ);
}

vec3 ComputeLight(vec3 worldPos, vec3 normal, vec3 lightP, vec3 lightC)
{
    vec3 toLight = lightP - worldPos;
    float dist   = length(toLight);
    vec3 L       = toLight / dist;

    float diff = max(dot(normal, L), 0.0);
    float atten = 1.0 / (1.0 + dist * dist * 0.001);

    return lightC * diff * atten * 1.5;
}

void main()
{
    vec2 uv = gl_FragCoord.xy / screenSize;

    vec4 baseColor = texture(baseTex, uv);
    if (baseColor.a <= 0.001)
        discard;

    vec3 normal = DecodeNormal(uv);
    vec4 depthData = texture(depthTex, uv);

    vec2 screen      = gl_FragCoord.xy;
    vec2 worldScreen = ScreenToWorldScreen(screen);

    vec3 worldPos = DepthToWorld(depthData, worldScreen);

    vec3 totalLight = vec3(0.25);

    for (int i = 0; i < lightCount; i++)
        totalLight += ComputeLight(worldPos, normal, lightPos[i], lightColor[i]);

    totalLight = clamp(totalLight, 0.0, 2.0);
    finalColor = vec4(baseColor.rgb * totalLight, baseColor.a);
}