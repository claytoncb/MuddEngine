#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;

uniform float yNorm;           // normalized worldY
uniform float maxZ;

uniform float zNorm;           // sprite origin Z normalized
uniform float worldY;          // true worldY
uniform float worldZBase;      // sprite.Position.Z
uniform float spriteHeight;    // world-space height of sprite

out vec4 finalColor;

void main()
{
    vec4 tex = texture(texture0, fragTexCoord);
    if (tex.a <= 0.0)
        discard;

    float worldZ;

    worldZ = worldZBase - fragTexCoord.y * spriteHeight * (1.0 - tex.g);

    float zOut = clamp(worldZ / maxZ, 0.0, 1.0);

    // R = parallax mask (tex.r), G = yNorm, B = per-pixel zNorm, A = alpha
    finalColor = vec4(tex.r, yNorm, zOut, tex.a);
}