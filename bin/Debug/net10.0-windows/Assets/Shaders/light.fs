#version 330 core

out vec4 finalColor;

uniform vec2 screenSize;
uniform vec2 cameraTarget;
uniform float cameraZoom;

uniform vec3 lightPos;
uniform float lightRadius;

// NEW
uniform vec3 lightColor;

void main()
{
    vec2 uv = gl_FragCoord.xy / screenSize;

    vec2 pixelScreen  = uv * screenSize;
    vec2 screenCenter = screenSize * 0.5;

    vec2 lightScreen = vec2(
        (lightPos.x - cameraTarget.x) * cameraZoom + screenCenter.x,
        (lightPos.y + cameraTarget.y) * cameraZoom + screenCenter.y
    );

    float dist = length(lightScreen - pixelScreen);
    float att  = clamp(1.0 - dist / (lightRadius * cameraZoom), 0.0, 1.0);
    att = att * att;

    // Apply color
    finalColor = vec4(lightColor * att, 1.0);
}