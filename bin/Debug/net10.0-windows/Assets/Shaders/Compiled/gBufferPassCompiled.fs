#version 330 core

uniform sampler2D u_BaseAtlas;
uniform sampler2D u_NormalsAtlas;
uniform sampler2D u_DepthAtlas;
uniform sampler2D u_SpriteData;
uniform int u_MaxSprites;
uniform int u_RowsPerSprite;
uniform vec2 screenSize;
uniform vec2 atlasSize;
uniform int   muddObjectCount;
uniform vec3 cameraPosition;
uniform float cameraZoom;
out vec4 finalColor;
const bool FLIP_ATLAS_Y = true;
const float isoScaleY = 0.5;

ivec2 spriteTexelCoord(int spriteIndex, int rowIndex)
{
    int linear = spriteIndex * u_RowsPerSprite + rowIndex;
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

vec4 fetchAtlasTexelI(sampler2D atlas, ivec2 p, ivec2 sz, bool flipY) {
    if (flipY) p.y = sz.y - 1 - p.y;
    p = clamp(p, ivec2(0), sz - ivec2(1));
    return texelFetch(atlas, p, 0);
}

ivec2 atlasISize_i() {
    return ivec2(atlasSize + 0.5);
}

void computeFragAndLayer(out ivec2 fragI, out int layer, out vec2 fragF) {
    fragI = ivec2(gl_FragCoord.xy);
    ivec2 screenISize = ivec2(screenSize + 0.5);
    int bandH = max(1, screenISize.y / 4);
    layer = clamp(fragI.y / bandH, 0, 3);
    ivec2 fragBandI = ivec2(fragI.x, fragI.y - layer * bandH);
    fragF = vec2(float(fragBandI.x), float(fragBandI.y));
}

void readBounds(int idx, out vec2 spriteBottomLeft, out vec2 visibleOffset, out vec2 visibleSize) {
    spriteBottomLeft = readVec2(u_SpriteData, idx, 1);
    visibleOffset    = readVec2(u_SpriteData, idx, 5);
    visibleSize      = readVec2(u_SpriteData, idx, 6);
}

ivec2 computePixelCoordI_fromLocal(vec2 atlasOrigin, vec2 sheetLocation, vec2 visibleOffset, vec2 visibleSize, vec2 local) {
    // convert relevant values to integer texel units and clamp local*size to [0..size-1]
    ivec2 visSizeI      = ivec2(visibleSize + 0.5);
    ivec2 visOffsetI    = ivec2(visibleOffset + 0.5);
    ivec2 atlasOriginI  = ivec2(atlasOrigin + 0.5);
    ivec2 sheetLocationI= ivec2(sheetLocation + 0.5);

    ivec2 texelInSprite = ivec2(floor(local * vec2(visSizeI)));
    texelInSprite = clamp(texelInSprite, ivec2(0), visSizeI - ivec2(1));

    return atlasOriginI + sheetLocationI + visOffsetI + texelInSprite;
}

vec4 sampleAtlasLayer(int layer, ivec2 pixelCoordI, ivec2 atlasISize) {
    switch (layer) {
        case 0: return fetchAtlasTexelI(u_BaseAtlas, pixelCoordI, atlasISize, FLIP_ATLAS_Y);
        case 1: return fetchAtlasTexelI(u_NormalsAtlas, pixelCoordI, atlasISize, FLIP_ATLAS_Y);
        case 2: return fetchAtlasTexelI(u_DepthAtlas, pixelCoordI, atlasISize, FLIP_ATLAS_Y);
        default: return vec4(0.0, 0.0, 0.0, 1.0);
    }
}

ivec2 computeTexelCoordI_forWorld(vec2 visibleOffset, vec2 visibleSize, vec2 local) {
    ivec2 visSizeI   = ivec2(visibleSize + 0.5);
    ivec2 visOffsetI = ivec2(visibleOffset + 0.5);
    ivec2 texelInSprite = ivec2(floor(local * vec2(visSizeI)));
    texelInSprite = clamp(texelInSprite, ivec2(0), visSizeI - ivec2(1));
    return visOffsetI + texelInSprite;
}



void main()
{
    if (muddObjectCount <= 0) discard;

    ivec2 atlasISize = atlasISize_i();

    ivec2 fragI;
    int layer;
    vec2 frag;
    computeFragAndLayer(fragI, layer, frag);

    for (int i = 0; i < muddObjectCount; ++i) {
        // bounds test (minimal pre-reads)
        vec2 spriteBottomLeft, visibleOffset, visibleSize;
        readBounds(i, spriteBottomLeft, visibleOffset, visibleSize);

        vec2 scaledVisible = visibleSize * cameraZoom;
        vec2 scaledOffset  = visibleOffset * cameraZoom;
        vec2 minB = spriteBottomLeft + scaledOffset;
        vec2 maxB = minB + scaledVisible;
        if (fragOutsideQuad(frag, minB, maxB)) continue;

        // atlas placement and local UV
        vec2 sheetLocation = readVec2(u_SpriteData, i, 3);
        vec2 atlasOrigin   = readVec2(u_SpriteData, i, 4);
        vec2 local = computeLocalUV(frag, minB, maxB);

        // integer atlas texel coordinate (maps local==1.0 to last texel)
        ivec2 pixelCoordI = computePixelCoordI_fromLocal(atlasOrigin, sheetLocation, visibleOffset, visibleSize, local);

        // sample base first (alpha test)
        vec4 src = fetchAtlasTexelI(u_BaseAtlas, pixelCoordI, atlasISize, FLIP_ATLAS_Y);
        if (isTransparent(src)) continue;

        // layer-specific output; defer extra reads for layer 3
        if (layer == 3) {
            vec3 worldPosBase = readVec3(u_SpriteData, i, 0);
            bool isFlat = readFloat(u_SpriteData, i, 7) > 0.5;
            ivec2 texelCoordI = computeTexelCoordI_forWorld(visibleOffset, visibleSize, local);
            finalColor = showPixelPositions(vec2(texelCoordI), worldPosBase, isFlat);
        } else {
            finalColor = sampleAtlasLayer(layer, pixelCoordI, atlasISize);
        }

        break; // topmost sprite found
    }

    if (finalColor.a <= EPS_ALPHA) discard;
}