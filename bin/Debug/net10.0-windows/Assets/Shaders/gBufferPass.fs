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
#include "gBufferHelpers.fs"
#include "dataTextureHelpers.fs"

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