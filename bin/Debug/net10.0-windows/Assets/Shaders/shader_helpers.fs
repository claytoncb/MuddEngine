const float EPS_ALPHA = 0.001;

bool fragOutsideQuad(vec2 f, vec2 a, vec2 b) {
    return f.x < a.x || f.x > b.x || f.y < a.y || f.y > b.y;
}

vec2 computeLocalUV(vec2 frag, vec2 minB, vec2 maxB) {
    return (frag - minB) / (maxB - minB);
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
    vec3 worldPos,
    vec3 normal,
    int lightCount,
    vec3 lightPos[8],
    float lightRadius[8],
    float lightIntensity[8],
    vec3 lightColorSRGB[8]
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
            spriteWorldPos.y + texelCoord.y,
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