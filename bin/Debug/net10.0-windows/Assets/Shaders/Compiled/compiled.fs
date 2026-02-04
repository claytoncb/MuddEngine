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
uniform vec2  cameraOffset;
uniform vec2  cameraTarget;
uniform float cameraZoom;



const int MAX_LIGHTS = 8;

// Light uniforms (must match C# sizes)
uniform int   lightCount;
uniform vec3  lightPositions[MAX_LIGHTS];
uniform float lightRadii[MAX_LIGHTS];
uniform float lightIntensities[MAX_LIGHTS];
uniform vec3  lightColors[MAX_LIGHTS];

out vec4 finalColor;


const float EPS_ALPHA = 0.001;

bool fragOutsideQuad(vec2 f, vec2 a, vec2 b) {
    return f.x < a.x || f.x > b.x || f.y < a.y || f.y > b.y;
}

vec2 computeLocalUV(vec2 f, vec2 a, vec2 b) {
    return (f - a) / (b - a);
}

vec2 pixelCoordFromLocal(vec2 o, vec2 s, vec2 l, vec2 vo, vec2 vs) {
    return o + s + vo + l * vs;
}

vec4 fetchAtlasTexel(sampler2D atlas, vec2 p, vec2 sz, bool flipY) {
    if (flipY) p.y = sz.y - 1.0 - p.y;
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
    vec3 wp, vec2 frag, vec3 N,
    int lc,
    vec3 lp[8], float lr[8], float li[8], vec3 lc0[8])
{
    vec3 r = vec3(0.02);
    for (int i = 0; i < lc; ++i) {
        vec3 L = lp[i] - wp;
        float d = length(L);
        if (d <= 0.0001) continue;
        float rad = lr[i];
        if (rad > 0.0 && d > rad) continue;

        vec3 lin = pow(lc0[i], vec3(2.2));
        float lum = dot(lin, vec3(0.2126,0.7152,0.0722));
        float f = lum < 0.0001 ? 1.0 : clamp(1.0/lum, 0.5, 2.0);
        vec3 col = pow(lin * f, vec3(1.0/2.2));

        float att = rad > 0.0 ? (1.0 - d/rad) : 1.0;
        float ndl = max(dot(N, normalize(L)), 0.0);
        r += col * (ndl * att * li[i]);
    }
    return clamp(r, 0.0, 4.0);
}

vec3 computePixelWorldPos(
    vec2 fw,
    vec3 wp, bool isFlat)
{
    vec3 b = wp;
    return (isFlat)
        ? vec3(fw.x, fw.y, b.z)
        : vec3(fw.x, b.y, fw.y);
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
// ------------------------------------------------------------
// helpers are concatenated before this file by ShaderLoader
// ------------------------------------------------------------

void main()
{
    if (muddObjectCount <= 0) discard;

    vec2 frag = gl_FragCoord.xy;
    vec4 accum = vec4(0.0);

    // flipY flag: true because your atlas is top-left authored and you flip before texelFetch
    const bool FLIP_ATLAS_Y = true;

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

        // If debugMode 4, compute lighting using normals + per-pixel world position
        vec4 litSrc = src;

        // --- DebugMode 4: sprite-local X/Y/Z visualization ---
        if (debugMode == 4)
        {
            //vec3 offs = computeSpriteLocalOffsets(i, local, visibleSizes[i], isFlat);
            //litSrc = vec4(offs, 1.0);
            litSrc = vec4(0.0,0.0,0.0, 1.0);
        }
        // --- DebugMode 0: full lighting using world-space per-pixel positions ---
        if (debugMode == 0)
        {
            // sample normal map at same atlas pixel
            vec3 normal = sampleNormalAtPixel(
                u_NormalsAtlas,
                pixelCoord,
                atlasSize,
                FLIP_ATLAS_Y
            );

            // screen → world (2D) using camera
            vec2 frag_world2D = (frag - cameraOffset) / cameraZoom + cameraTarget;

            // use the existing helper to get per-pixel world position
            vec3 pixelWorldPos = computePixelWorldPos(
                frag_world2D,
                worldPosBase, 
                isFlat
            );

            vec3 lighting = computeLightingWithNormals(
                pixelWorldPos,
                frag,
                normal,
                lightCount,
                lightPositions,
                lightRadii,
                lightIntensities,
                lightColors
            );

            litSrc = vec4(src.rgb * lighting, src.a);
        }

        accum = compositeOver(litSrc, accum);

        if (accum.a >= 0.999)
            break;
    }

    if (accum.a <= 0.001)
        discard;

    finalColor = accum;
}