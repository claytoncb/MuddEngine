#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;
uniform float spriteID;

out vec4 finalColor;

void main()
{
    vec4 tex = texture(texture0, fragTexCoord);
    if (tex.a <= 0.0)
        discard;

    finalColor = vec4(spriteID, 0.0, 0.0, tex.a);
}