#version 330 core

#define MAX_LIGHTS  32
#define MAX_SPRITES 512

uniform sampler2D baseTex;
uniform sampler2D normalTex;
uniform sampler2D depthTex;
uniform sampler2D metaTex;

uniform vec2  screenSize;

uniform vec3  lightPos[MAX_LIGHTS];
uniform vec3  lightColor[MAX_LIGHTS];
uniform int   lightCount;

uniform vec3  spriteWorldPosArray[MAX_SPRITES];
uniform int   spriteCount;

out vec4 finalColor;

// ------------------------------------------------------------
// Sprite ID decoding (kept in case you still want it later)
// ------------------------------------------------------------
int DecodeSpriteID(float idNorm)
{
    int id = int(idNorm * float(spriteCount - 1) + 0.5);
    return clamp(id, 0, spriteCount - 1);
}

int GetSpriteID(vec2 uv)
{
    float idNorm = texture(metaTex, uv).r;
    return DecodeSpriteID(idNorm);
}

vec3 GetSpritePosBase(vec2 uv)
{
    return spriteWorldPosArray[GetSpriteID(uv)];
}

// ------------------------------------------------------------
// World reconstruction (kept, even if not strictly needed yet)
// ------------------------------------------------------------
vec3 ReconstructPixelWorldPos(vec2 uv)
{
    vec4 d = texture(depthTex, uv);

    float xOff = d.r;
    float yOff = d.g;
    float zOff = d.b;

    vec3 base = GetSpritePosBase(uv);

    float worldX = base.x + xOff * 128.0;
    float worldY = base.y + yOff * 128.0;
    float worldZ = base.z - zOff * 128.0;

    return vec3(worldX, worldY, worldZ);
}

// ------------------------------------------------------------
// Simple lighting (no shadows)
// ------------------------------------------------------------
vec3 ComputeLighting(vec3 pixelWorldPos, vec3 normal, vec3 lightWorldPos, vec3 lightCol)
{
    vec3 toLight = lightWorldPos - pixelWorldPos;
    float dist   = length(toLight);
    vec3 L       = toLight / max(dist, 0.0001);

    float NdotL = max(dot(normal, L), 0.0);
    float atten = 1.0 / (1.0 + dist * dist * 0.0005);

    return lightCol * NdotL * atten * 5.0;
}

// ------------------------------------------------------------
// Main
// ------------------------------------------------------------
void main()
{
    vec2 uv = gl_FragCoord.xy / screenSize;

    vec4 baseColor = texture(baseTex, uv);
    if (baseColor.a <= 0.001)
        discard;

    vec3 normal        = normalize(texture(normalTex, uv).rgb * 2.0 - 1.0);
    vec3 pixelWorldPos = ReconstructPixelWorldPos(uv);

    vec3 totalLight = vec3(0.05);

    for (int i = 0; i < lightCount; i++)
    {
        vec3 direct = ComputeLighting(pixelWorldPos, normal, lightPos[i], lightColor[i]);
        totalLight += direct;
    }

    totalLight = clamp(totalLight, 0.0, 2.0);
    finalColor = vec4(baseColor.rgb * totalLight, baseColor.a);
}