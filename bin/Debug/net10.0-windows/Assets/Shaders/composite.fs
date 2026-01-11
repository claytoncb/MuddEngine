#version 330 core

#define MAX_LIGHTS 32

uniform sampler2D baseTex;
uniform sampler2D normalTex;
uniform sampler2D depthTex;

uniform vec2 screenSize;
uniform float maxY;
uniform float maxZ;

// Camera2D uniforms
uniform vec2 cameraTarget;
uniform vec2 cameraOffset;
uniform float cameraZoom;
uniform vec3 cameraPos;

// Lights
uniform vec3 lightPos[MAX_LIGHTS];
uniform vec3 lightColor[MAX_LIGHTS];
uniform int lightCount;

out vec4 finalColor;

void main()
{
    vec2 uv = gl_FragCoord.xy / screenSize;

    vec4 baseColor = texture(baseTex, uv);
    vec3 raw = texture(normalTex, uv).rgb * 2.0 - 1.0;

    // Normal map → world space
    vec3 normal = vec3(
        raw.r,  // world X
        raw.g,  // world Y (toward viewer)
        raw.b   // world Z (up)
    );
    normal = normalize(normal);

    // Depth buffer → world Y/Z
    vec4 depthData =texture(depthTex, uv);
    bool uprightSprite = depthData.r==1.0;
    float spriteYNorm = depthData.g;
    float spriteZNorm = depthData.b;

    float worldY = ((spriteYNorm - 0.5) * (2.0 * maxY)) + cameraPos.y;
    
    float worldZ = spriteZNorm * maxZ;

    // Undo Camera2D transform
    vec2 screen = gl_FragCoord.xy;
    vec2 worldScreen = (screen - cameraOffset) / cameraZoom + cameraTarget;

    float worldX = worldScreen.x;
    if (!uprightSprite)
    {
        float projectedY = worldScreen.y - 2*cameraTarget.y;
        worldY = (-projectedY - worldZ * 8.0) * 2.0;
        worldY += 1028.0;  // tune this value

    }




    vec3 pixelWorldPos = vec3(worldX, worldY, worldZ);

    // Ambient
    vec3 totalLight = vec3(0.25);

    for (int i = 0; i < lightCount; i++)
    {
        vec3 toLight = lightPos[i] - pixelWorldPos;
        vec3 L = normalize(toLight);

        float diff = max(dot(normal, L), 0.0);

        float dist = length(toLight);
        float atten = 1.0 / (1.0 + dist * dist * 0.001); // smooth, non-flicker attenuation

        totalLight += lightColor[i] * diff * atten * 1.5;
    }

    totalLight = clamp(totalLight, 0.0, 2.0);

    finalColor = vec4(baseColor.rgb * totalLight, baseColor.a);
}