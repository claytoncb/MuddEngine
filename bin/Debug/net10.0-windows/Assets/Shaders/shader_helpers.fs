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