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

#include "dataTextureHelpers.fs"
#include "gBufferHelpers.fs"

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