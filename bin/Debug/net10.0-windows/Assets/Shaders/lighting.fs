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