#version 330 core

// ------------------------------------------------------------
// composite.fs — main composite shader
// shader_helpers.fs is concatenated before this body.
// ------------------------------------------------------------

// --- Uniforms ------------------------------------------------

uniform sampler2D u_BaseAtlas;
uniform sampler2D u_NormalsAtlas;
uniform sampler2D u_DepthAtlas;

uniform sampler2D u_SpriteData; // RGBA32F data texture (8 x u_MaxSprites)
uniform int u_MaxSprites;       // equals MAX_MUDD_OBJECTS on the CPU side


uniform int  debugMode;      // debug mode 1,2,3,4
uniform vec2 screenSize;     // window size in pixels
uniform vec2 atlasSize;      // atlas size in pixels (width, height)
uniform int   muddObjectCount;

uniform vec3 cameraPosition;
uniform vec2  cameraOffset;
uniform vec2  cameraTarget;
uniform float cameraZoom;



const int MAX_LIGHTS = 16;

// Light uniforms (must match C# sizes)
uniform int   lightCount;
uniform vec3  lightPositions[MAX_LIGHTS];
uniform float lightRadii[MAX_LIGHTS];
uniform float lightIntensities[MAX_LIGHTS];
uniform vec3  lightColors[MAX_LIGHTS];

out vec4 finalColor;
const bool FLIP_ATLAS_Y = true;
const float isoScaleY = 0.5;
// ------------------------------------------------------------
// helpers are concatenated before this file by ShaderLoader
// ------------------------------------------------------------
const float EPS_ALPHA = 0.001;

bool fragOutsideQuad(vec2 f, vec2 a, vec2 b) {
    return f.x < a.x || f.x > b.x || f.y < a.y || f.y > b.y;
}

vec2 computeLocalUV(vec2 frag, vec2 minB, vec2 maxB) {
    return (frag - minB) / (maxB - minB);
}

vec2 pixelCoordFromLocal(vec2 atlasOrigin, vec2 sheetLocation, vec2 local, vec2 visibleOffset, vec2 visibleSize) {
    return atlasOrigin + sheetLocation + visibleOffset + local * visibleSize;
}

vec4 fetchAtlasTexel(sampler2D atlas, vec2 p, vec2 sz, bool flipY) {
    if (flipY) p.y = sz.y - p.y;
    ivec2 t = clamp(ivec2(p), ivec2(0), ivec2(sz) - ivec2(1));
    return texelFetch(atlas, t, 0);
}

vec4 compositeOver(vec4 s, vec4 d) {
    return s + (1.0 - s.a) * d;
}

bool isTransparent(vec4 c) {
    return c.a <= EPS_ALPHA;
}

vec3 decodeNormal(vec4 t) {
    ivec3 c = ivec3(t.rgb * 255.0 + 0.5);
    bool bad =
        c == ivec3(0xD0,0x80,0x5C) ||
        c == ivec3(0x38,0x68,0x90) ||
        c == ivec3(0xEC,0xC8,0x78) ||
        c == ivec3(0x23,0x17,0x17) ||
        c == ivec3(0x7C,0x9C,0xDC) ||
        c == ivec3(0x6C,0x6C,0x6C);
    if (bad) return vec3(0.0,-1.0,0.0);
    return normalize(t.rgb * 2.0 - 1.0);
}

vec3 sampleNormalAtPixel(sampler2D n, vec2 p, vec2 sz, bool flipY) {
    return decodeNormal(fetchAtlasTexel(n, p, sz, flipY));
}

vec3 computeLightingWithNormals(
    vec3 worldPos,
    vec3 normal,
    int lightCount,
    vec3 lightPos[MAX_LIGHTS],
    float lightRadius[MAX_LIGHTS],
    float lightIntensity[MAX_LIGHTS],
    vec3 lightColorSRGB[MAX_LIGHTS]
){
    vec3 accumulated = vec3(0.02);   // ambient floor

    for (int i = 0; i < lightCount; ++i)
    {
        // Vector from surface → light
        vec3 toLight = lightPos[i] - worldPos;
        float dist = length(toLight);
        if (dist <= 0.0001) continue;

        float radius = lightRadius[i];
        if (radius > 0.0 && dist > radius) continue;

        // Convert sRGB → linear
        vec3 lightColorLinear = pow(lightColorSRGB[i], vec3(2.2));

        // Perceived brightness normalization
        float luminance = dot(lightColorLinear, vec3(0.2126, 0.7152, 0.0722));
        float brightnessFactor = (luminance < 0.0001)
            ? 1.0
            : clamp(1.0 / luminance, 0.5, 2.0);

        vec3 balancedColor = pow(lightColorLinear * brightnessFactor, vec3(1.0 / 2.2));

        // Attenuation (your original linear falloff)
        float attenuation = (radius > 0.0) ? (1.0 - dist / radius) : 1.0;

        // Lambertian diffuse
        float NdotL = max(dot(normal, normalize(toLight)), 0.0);

        // --- NEW: ambient contribution based only on distance ---
        float ambientFactor = attenuation * 0.15;   // tweak strength here
        vec3 ambientFromLight = balancedColor * ambientFactor;

        // Accumulate both
        accumulated += ambientFromLight * lightIntensity[i];
        accumulated += balancedColor * (NdotL * attenuation * lightIntensity[i]);
    }

    return clamp(accumulated, 0.0, 4.0);
}


vec3 computePixelWorldPos(
    vec2 texelCoord,      // pixel inside sprite, in texels
    vec3 spriteWorldPos,  // bottom-left-front of sprite
    bool isFlat
){
    if (isFlat) {
        // flat on ground
        return vec3(
            spriteWorldPos.x + texelCoord.x,
            spriteWorldPos.y + texelCoord.y / isoScaleY,
            spriteWorldPos.z
        );
    } else {
        // vertical billboard
        return vec3(
            spriteWorldPos.x + texelCoord.x,
            spriteWorldPos.y,
            spriteWorldPos.z + texelCoord.y
        );
    }
}


// Convert (spriteIndex, rowIndex) to texel coordinates
const int ROWS_PER_SPRITE   = 8;              // or whatever you use

ivec2 spriteTexelCoord(int spriteIndex, int rowIndex)
{
    int linear = spriteIndex * ROWS_PER_SPRITE + rowIndex;
    int x = linear % u_MaxSprites;
    int y = linear / u_MaxSprites;
    return ivec2(x, y);
}


vec4 readRow(sampler2D tex, int spriteIndex, int rowIndex)
{
    return texelFetch(tex, spriteTexelCoord(spriteIndex, rowIndex), 0);
}

float readFloat(sampler2D tex, int spriteIndex, int rowIndex)
{
    return readRow(tex, spriteIndex, rowIndex).r;
}

vec2 readVec2(sampler2D tex, int spriteIndex, int rowIndex)
{
    return readRow(tex, spriteIndex, rowIndex).rg;
}

vec3 readVec3(sampler2D tex, int spriteIndex, int rowIndex)
{
    return readRow(tex, spriteIndex, rowIndex).rgb;
}

vec4 readVec4(sampler2D tex, int spriteIndex, int rowIndex)
{
    return readRow(tex, spriteIndex, rowIndex);
}
vec4 print()
{
    if (muddObjectCount <= 0) discard;

    // Protect against zero height
    float h = max(screenSize.y, 1.0);

    // Height of each strip in pixels (may be <1 if many objects)
    float stripH = h / float(max(muddObjectCount, 1));

    // Compute index from fragment Y (gl_FragCoord.y origin is bottom-left)
    int idx = int(floor(gl_FragCoord.y / stripH));
    idx = clamp(idx, 0, muddObjectCount - 1);

    // Read world position for this index (uniform array uploaded from CPU)
    vec3 wp       = readVec3(u_SpriteData, idx, 0);

    // Map world coords -> color. Tune scale/bias to your world extents.
    // Example mapping: shift X into [0..1], scale Y down, use Z directly (clamped).
    vec3 col = vec3(
        clamp((wp.x + 800.0) / 2000.0, 0.0, 1.0),
        clamp(wp.y / 32.0, 0.0, 1.0),
        clamp((wp.z + 128.0) / 256.0, 0.0, 1.0)
    );

    // Optional thin separators between strips for readability
    float localY = mod(gl_FragCoord.y, stripH);
    float edgePx = 1.0; // pixels
    if (stripH >= 2.0 && (localY < edgePx || localY > stripH - edgePx))
        col *= 0.15;


    return vec4(col, 1.0);
}
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
vec4 lighting(vec2 pixelCoord, vec2 texelCoord, vec3 worldPosBase, bool isFlat, vec4 src, bool FLIP_ATLAS_Y) {
    // sample normal map at same atlas pixel
    vec3 normal = sampleNormalAtPixel(
        u_NormalsAtlas,
        pixelCoord,
        atlasSize,
        FLIP_ATLAS_Y
    );


    // use the existing helper to get per-pixel world position
    vec3 pixelWorldPos = computePixelWorldPos(
        texelCoord,
        worldPosBase, 
        isFlat
    );


    vec3 lighting = computeLightingWithNormals(
        pixelWorldPos,
        normal,
        lightCount,
        lightPositions,
        lightRadii,
        lightIntensities,
        lightColors
    );

    return vec4(src.rgb * lighting, src.a);
}
vec4 showPixelPositions(vec2 texelCoord, vec3 worldPosBase, bool isFlat)
{
    // world offset from camera center
    vec3 pixelWorldPos = computePixelWorldPos(
        texelCoord,
        worldPosBase, 
        isFlat
    );

    vec3 offset = pixelWorldPos - cameraPosition;

    float worldWidth  = screenSize.x;
    float worldHeight = screenSize.y;

    float nx = offset.x / worldWidth  + 0.5;
    float ny = offset.y / worldHeight + 0.5;
    float nz = offset.z / 32.0;

    return vec4(nx, ny, nz, 1.0);
}


void main()
{
    if (muddObjectCount <= 0) discard;

    vec2 frag = gl_FragCoord.xy;

    for (int i = 0; i < muddObjectCount; ++i)
    {
        vec3 worldPosBase       = readVec3(u_SpriteData, i, 0);
        vec2 spriteBottomLeft   = readVec2(u_SpriteData, i, 1);
        vec2 frameSize          = readVec2(u_SpriteData, i, 2);
        vec2 sheetLocation      = readVec2(u_SpriteData, i, 3);
        vec2 atlasOrigin        = readVec2(u_SpriteData, i, 4);
        vec2 visibleOffset      = readVec2(u_SpriteData, i, 5);
        vec2 visibleSize        = readVec2(u_SpriteData, i, 6);
        bool isFlat             = readFloat(u_SpriteData, i, 7)>0.5;
        
        // screen-space sizes (scaled by camera zoom)
        vec2 scaledFrame   = frameSize * cameraZoom;
        vec2 scaledVisible = visibleSize * cameraZoom;
        vec2 scaledOffset  = visibleOffset * cameraZoom;

        // quad bounds for the visible rect inside the frame
        vec2 minB = spriteBottomLeft + scaledOffset;
        vec2 maxB = minB + scaledVisible;

        // local (0..1) inside the scaled visible quad
        vec2 local = computeLocalUV(frag, minB, maxB);
        vec2 texelCoord = visibleOffset + local * visibleSize;
        texelCoord = floor(texelCoord);

        if (debugMode == 6)
        {
            finalColor = print();
            return;
        }
        if (fragOutsideQuad(frag, minB, maxB) && debugMode != 6) continue;

        // pixel-space coordinate inside the atlas (unflipped)
        vec2 pixelCoord = pixelCoordFromLocal(
            atlasOrigin,
            sheetLocation,
            local,
            visibleOffset,
            visibleSize
        );

        // choose atlas based on debugMode
        vec4 src;
        if (debugMode == 1)
        {
            src = fetchAtlasTexel(u_BaseAtlas, pixelCoord, atlasSize, FLIP_ATLAS_Y);
        }
        else if (debugMode == 2)
        {
            src = fetchAtlasTexel(u_NormalsAtlas, pixelCoord, atlasSize, FLIP_ATLAS_Y);
        }
        else if (debugMode == 3)
        {
            src = fetchAtlasTexel(u_DepthAtlas, pixelCoord, atlasSize, FLIP_ATLAS_Y);
        }
        else if (debugMode == 4)
        {
            // base color always sampled from base atlas for lighting
            src = fetchAtlasTexel(u_BaseAtlas, pixelCoord, atlasSize, FLIP_ATLAS_Y);
        }
        else
        {
            // default to base atlas if debugMode is out of range
            src = fetchAtlasTexel(u_BaseAtlas, pixelCoord, atlasSize, FLIP_ATLAS_Y);
        }

        if (isTransparent(src))
            continue;

        vec4 litSrc = src;

        // --- DebugMode 4: sprite-local X/Y/Z visualization ---
        if (debugMode == 4)
        {
            litSrc = showPixelPositions(texelCoord, worldPosBase, isFlat);
        }
        if (debugMode == 7)
        {
            vec3 pixelWorldPos = computePixelWorldPos(texelCoord, worldPosBase, isFlat);
            vec3 normal = sampleNormalAtPixel(
                u_NormalsAtlas,
                pixelCoord,
                atlasSize,
                FLIP_ATLAS_Y
            );

            float shadowFactor = computeShadowFactor_SingleBlocker(
                pixelWorldPos,
                normal,
                i
            );

            finalColor = vec4(vec3(1.0 - shadowFactor), 1.0);
            return;
        }

        // --- DebugMode 0: full lighting using world-space per-pixel positions ---
        if (debugMode == 0)
        {
            litSrc = lighting(pixelCoord, texelCoord, worldPosBase, isFlat, src, FLIP_ATLAS_Y);
        }

        finalColor = litSrc;
        break;
    }

    if (finalColor.a <= 0.001)
        discard;
}