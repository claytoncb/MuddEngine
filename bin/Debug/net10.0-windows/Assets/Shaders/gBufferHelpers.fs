
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