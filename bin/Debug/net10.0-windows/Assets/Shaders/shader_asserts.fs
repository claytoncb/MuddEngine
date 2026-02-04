// GPU ASSERT SYSTEM
// -------------------------------------------------------------

// Writes a bright magenta pixel and aborts the sprite loop.
// You can expand this to encode error codes in RGB.
void gpuAssert(bool condition, int spriteIndex, int row, int code, inout bool abortFlag) {
    if (!condition && !abortFlag) {
        // Encode error info into the output color
        // R = sprite index / 255
        // G = row / 255
        // B = code / 255
        // A = 1
        finalColor = vec4(
            float(spriteIndex) / 255.0,
            float(row) / 255.0,
            float(code) / 255.0,
            1.0
        );
        abortFlag = true;
    }
}