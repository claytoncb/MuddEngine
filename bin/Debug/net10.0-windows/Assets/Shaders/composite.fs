#version 330 core

#define MAX_LIGHTS  32
#define MAX_SPRITES 512

uniform sampler2D baseTex;    // Albedo/color
uniform sampler2D normalTex;  // Normals encoded in RGB
uniform sampler2D depthTex;   // Sprite-relative depth offsets (xOff, yOff, zOff)
uniform sampler2D metaTex;    // Sprite ID buffer

uniform vec2  screenSize;

uniform vec2  cameraTarget;   // World-space camera center (XY)
uniform vec2  cameraOffset;   // Screen-space offset (e.g. half screen size)
uniform float cameraZoom;
uniform float cameraRotation; // (unused here)

uniform vec3  lightPos[MAX_LIGHTS];
uniform vec3  lightColor[MAX_LIGHTS];
uniform int   lightCount;

uniform vec3  spriteWorldPosArray[MAX_SPRITES]; // World-space sprite bases
uniform int   spriteCount;

out vec4 finalColor;

// ------------------------------------------------------------
// Sprite ID decoding
// ------------------------------------------------------------

// CPU encodes: shiftedID / (spriteCount + 1), where shiftedID = 1..spriteCount.
int DecodeSpriteID(float idNorm)
{
    int id = int(idNorm * float(spriteCount + 1) + 0.5);
    // 0 = background, 1..spriteCount = sprites
    return clamp(id, 0, spriteCount);
}

int GetSpriteID(vec2 uv)
{
    float idNorm = texture(metaTex, uv).r;
    return DecodeSpriteID(idNorm);
}

vec3 GetSpritePosBase(vec2 uv)
{
    int spriteID = GetSpriteID(uv);
    if (spriteID == 0)
        return vec3(0.0); // background

    // spriteID 1 → index 0, etc.
    return spriteWorldPosArray[spriteID - 1];
}

// ------------------------------------------------------------
// World reconstruction (sprite-relative, using UV)
// ------------------------------------------------------------

vec3 ReconstructPixelWorldPos(vec2 uv)
{
    vec4 depthSample = texture(depthTex, uv);

    float xOff = depthSample.r;
    float yOff = depthSample.g;
    float zOff = depthSample.b;

    vec3 base = GetSpritePosBase(uv);

    // 128.0 is the world footprint scale for this sprite.
    float worldX = base.x + xOff * 128.0;
    float worldY = base.y + yOff * 128.0;
    float worldZ = base.z + zOff * 128.0;

    return vec3(worldX, worldY, worldZ);
}

// ------------------------------------------------------------
// Lighting
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
// World → screen projection (matching depth/meta pass)
// ------------------------------------------------------------

vec2 ScreenFromWorld(vec2 worldXY)
{
    vec2 adjustedCamera = vec2(cameraTarget.x,-cameraTarget.y);
    return (worldXY - adjustedCamera) * cameraZoom + cameraOffset;
}

// Encode world position into the same XY used by the depth/meta pass.
vec2 EncodeDepthXY(vec3 worldPos)
{
    // Depth pass currently uses X = worldX, Y = -worldY.
    return vec2(worldPos.x, worldPos.y);
}
bool isBetweenScreenHeight(vec3 pixel, vec3 blocker, vec3 light, float maxDist)
{
    // 1. Work in screen plane: X (right), Y (up on screen) == your X,Z
    vec2 pixel2D   = vec2(pixel.x,   pixel.z);
    vec2 blocker2D = vec2(blocker.x, blocker.z);
    vec2 light2D   = vec2(light.x,   light.z);

    vec2 ray2D   = light2D - pixel2D;
    float rayLen = length(ray2D);
    if (rayLen <= 0.0001)
        return false;

    vec2 rayDir2D = ray2D / rayLen;

    // 2. Project blocker onto 2D ray in screen space
    vec2 v2D = blocker2D - pixel2D;
    float t  = dot(v2D, rayDir2D);

    // Must lie between pixel and light in screen space
    if (t <= 0.0 || t >= rayLen)
        return false;

    // 3. Lateral distance in screen space
    float distToRay = length(v2D - rayDir2D * t);
    if (distToRay > maxDist)
        return false;

    // 4. Heightfield rule: Z (height) between pixel and light
    if (blocker.z <= pixel.z)
        return false;
    if (blocker.z >= light.z)
        return false;

    return true;
}
// ------------------------------------------------------------
// Shadow raymarch
// ------------------------------------------------------------

// Returns 1.0 if the pixel is in shadow from this light, 0.0 otherwise.
float TraceShadow(vec3 pixelWorldPos, int pixelSpriteID, vec3 lightWorldPos)
{
    // Pixel above or at light height cannot be shadowed.
    if (pixelWorldPos.z >= lightWorldPos.z)
        return 0.0;

    vec3 rayToLight   = lightWorldPos - pixelWorldPos;
    float rayLength   = length(rayToLight);
    vec3 rayDirection = rayToLight / max(rayLength, 0.0001);

    // Nearly horizontal rays won't be meaningfully occluded by this heightfield.
    if (abs(rayDirection.z) < 0.0001)
        return 0.0;

    const int   STEPS = 64;
    const float BIAS  = 1.0;

    for (int i = 0; i < STEPS; i++)
    {
        // Sample along the ray from pixel → light.
        float t = (float(i) / float(STEPS)) * rayLength;
        if (t >= rayLength)
            break;

        vec3 sampleWorldPos = pixelWorldPos + rayDirection * t;

        // Project sample into depth/meta UV space.
        vec2 sampleProjectedXY = EncodeDepthXY(sampleWorldPos);
        vec2 sampleScreenPos   = ScreenFromWorld(sampleProjectedXY);
        vec2 sampleUV          = sampleScreenPos / screenSize;

        // Skip samples off-screen.
        if (sampleUV.x < 0.0 || sampleUV.x > 1.0 ||
            sampleUV.y < 0.0 || sampleUV.y > 1.0)
        {
            continue;
        }

        int blockerSpriteID = GetSpriteID(sampleUV);

        // Background cannot block.
        if (blockerSpriteID == 0)
            continue;

        // Skip self-shadowing.
        if (blockerSpriteID == pixelSpriteID)
            continue;

        vec3 blockerWorldPos = ReconstructPixelWorldPos(sampleUV);

        // Heightfield occlusion: blocker must be above the sample and below the light.
        if (isBetweenScreenHeight(pixelWorldPos, blockerWorldPos, lightWorldPos, 1.5))
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
    vec2 screenPos = gl_FragCoord.xy;
    vec2 uv        = screenPos / screenSize;

    vec4 baseColor = texture(baseTex, uv);
    if (baseColor.a <= 0.001)
        discard;

    vec3 normal        = normalize(texture(normalTex, uv).rgb * 2.0 - 1.0);
    vec3 pixelWorldPos = ReconstructPixelWorldPos(uv);
    int  pixelSpriteID = GetSpriteID(uv);

    vec3 totalLight = vec3(0.05); // ambient

    for (int i = 0; i < lightCount; i++)
    {
        vec3 lightWorldPos = lightPos[i];

        float shadow       = TraceShadow(pixelWorldPos, pixelSpriteID, lightWorldPos);
        float shadowFactor = 1.0 - shadow;

        vec3 directLight = ComputeLighting(pixelWorldPos, normal, lightWorldPos, lightColor[i]);
        totalLight += directLight * shadowFactor;
    }

    totalLight = clamp(totalLight, 0.0, 2.0);
    finalColor = vec4(baseColor.rgb * totalLight, baseColor.a);
}