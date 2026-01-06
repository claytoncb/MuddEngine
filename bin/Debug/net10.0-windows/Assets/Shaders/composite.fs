#version 330

in vec2 fragTexCoord;

uniform sampler2D baseTexture;
uniform sampler2D lightTexture;

out vec4 finalColor;

void main()
{
    vec4 base = texture(baseTexture, fragTexCoord);
    vec4 lightTex = texture(lightTexture, fragTexCoord);
    vec3 light = lightTex.rgb; // ignore alpha


    vec3 result = base.rgb * light;

    finalColor = vec4(result, base.a);
}