#version 330 core

out vec4 finalColor;

uniform sampler2D u_GBuffer;
uniform vec2 screenSize;
uniform vec3 cameraPosition;
uniform float cameraZoom;

const int MAX_LIGHTS = 16;
uniform int   lightCount;
uniform vec3  lightPositions[MAX_LIGHTS];
uniform float lightRadii[MAX_LIGHTS];
uniform float lightIntensities[MAX_LIGHTS];
uniform vec3  lightColors[MAX_LIGHTS];

const bool FLIP_ATLAS_Y = true;
const float isoScaleY = 0.5;

ivec2 bandTexel(ivec2 pix, int band, ivec2 screenISize) {
    return pix + ivec2(0, band * screenISize.y);
}

vec4 fetchBandTexel(sampler2D buf, ivec2 pix, int band, ivec2 screenISize) {
    ivec2 t = bandTexel(pix, band, screenISize);
    return texelFetch(buf, t, 0);
}

vec3 decodeWorldPosFromColor(vec3 enc, vec3 camPos) {
    float worldWidth  = screenSize.x;
    float worldHeight = screenSize.y;
    vec3 worldPos;
    worldPos.x = camPos.x + (enc.x - 0.5) * worldWidth;
    worldPos.y = camPos.y + (enc.y - 0.5) * worldHeight;
    worldPos.z = camPos.z + enc.z * 32.0;
    return worldPos;
}

void main() {
    ivec2 pix = ivec2(gl_FragCoord.xy);
    ivec2 screenISize = ivec2(screenSize + 0.5);
    vec4 worldPosColor = fetchBandTexel(u_GBuffer, pix, 3, screenISize);
    vec4 depth = fetchBandTexel(u_GBuffer, pix, 2, screenISize);
    vec4 normal = fetchBandTexel(u_GBuffer, pix, 1, screenISize);
    vec4 albedo = fetchBandTexel(u_GBuffer, pix, 0, screenISize);
    vec3 pixelWorldPos = decodeWorldPosFromColor(worldPosColor.rgb, cameraPosition);

    finalColor = albedo;
}