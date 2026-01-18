#version 330

in vec2 fragTexCoord;
uniform sampler2D texture0;
uniform vec2 spriteAtlasPos;
uniform float frameSize;
uniform vec2 atlasSize;
out vec4 finalColor;

void main()
{
    vec4 tex = texture(texture0, fragTexCoord);
    if (tex.a <= 0.0)
        discard;
    vec2 atlasPixel = fragTexCoord * atlasSize;
    vec2 framePixelOrigin = spriteAtlasPos * frameSize;
    vec2 localPixel = atlasPixel - framePixelOrigin;
    vec2 localUV = localPixel / frameSize;

    float xOffset = localUV.x * 128.0;
    float xOut    = clamp(xOffset / 128.0, 0.0, 1.0);

    float worldZ  = localUV.y * 128.0 * (1.0 - tex.g);
    float zOut    = clamp(worldZ / 128.0, 0.0, 1.0);

    finalColor = vec4(xOut, 1.0 - tex.r, zOut, tex.a);
}