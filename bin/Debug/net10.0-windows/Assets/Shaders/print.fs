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