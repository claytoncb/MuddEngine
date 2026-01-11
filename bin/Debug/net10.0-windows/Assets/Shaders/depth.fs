#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;   // sprite sheet
uniform float zNorm;      // 0..1 depth value
uniform float yNorm;      // 0..1 depth value

out vec4 finalColor;

void main()
{
    vec4 tex = texture(texture0, fragTexCoord);

    // If fully transparent, discard
    if (tex.a <= 0.0)
        discard;

    // RGB = depth, A = sprite alpha
    finalColor = vec4(0.0, yNorm, zNorm, tex.a);
}