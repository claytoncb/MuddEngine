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

vec2 computeLocalUV(vec2 frag, vec2 minB, vec2 maxB) {
    return (frag - minB) / (maxB - minB);
}

bool fragOutsideQuad(vec2 f, vec2 a, vec2 b) {
    return f.x < a.x || f.x > b.x || f.y < a.y || f.y > b.y;
}

vec2 pixelCoordFromLocal(vec2 atlasOrigin, vec2 sheetLocation, vec2 local, vec2 visibleOffset, vec2 visibleSize) {
    return atlasOrigin + sheetLocation + visibleOffset + local * visibleSize;
}

vec4 fetchAtlasTexel(sampler2D atlas, vec2 p, vec2 sz, bool flipY) {
    if (flipY) p.y = sz.y - p.y;
    ivec2 t = clamp(ivec2(p), ivec2(0), ivec2(sz) - ivec2(1));
    return texelFetch(atlas, t, 0);
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

void main()
{
    if (muddObjectCount <= 0) discard;
    vec2 frag = gl_FragCoord.xy;
    int layer = int(floor((frag.y / screenSize.y) * 4));
    frag.y = mod(frag.y, screenSize.y/4);
    
    
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

        if (fragOutsideQuad(frag, minB, maxB) && debugMode != 6) continue;
        
        // pixel-space coordinate inside the atlas (unflipped)
        vec2 pixelCoord = pixelCoordFromLocal(
            atlasOrigin,
            sheetLocation,
            local,
            visibleOffset,
            visibleSize
        );
        vec4 src= fetchAtlasTexel(u_BaseAtlas, pixelCoord, atlasSize, FLIP_ATLAS_Y);
        if (isTransparent(src))
            continue;
        if (layer == 0) 
            finalColor = src;
        else
            finalColor = vec4(0.0,0.0,0.0,1.0);
        break;
    }

    if (finalColor.a <= 0.001)
        discard;
}