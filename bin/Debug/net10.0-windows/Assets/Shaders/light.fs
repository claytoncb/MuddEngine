#version 330

in vec2 fragTexCoord;

uniform sampler2D normalMap;

// World positions
// X = horizontal, Y = depth, Z = height
uniform vec3 lightPos;
uniform vec3 spritePos;

uniform vec3  lightColor;
uniform float lightRadius;
uniform float lightIntensity;

uniform float specularStrength;
uniform float shininess;

uniform float rimIntensity;
uniform float rimPower;
uniform vec3 rimColor;

// Global minimum brightness (0.0 = no ambient, 1.0 = full bright)
uniform float ambient;

out vec4 finalColor;

void main()
{
    // --- NORMAL MAP ---
// --- NORMAL MAP ---
vec4 nm = texture(normalMap, fragTexCoord);

// Discard transparent pixels so they don't emit light
if (nm.a < 0.01)
    discard;

// Convert normal from [0,1] to [-1,1]
vec3 n = nm.rgb * 2.0 - 1.0;

    float nx = -n.r;
    float ny = n.b;
    float nz = n.g;

    vec3 normal = normalize(vec3(nx, ny, nz));

    // --- LIGHT VECTOR ---
    vec3 L = vec3(
        lightPos.x - spritePos.x,
        lightPos.y - spritePos.y,
        lightPos.z - spritePos.z
    );

    float dist = length(L);
    vec3 L3 = normalize(L);

    // --- RIM LIGHTING (Hollow Knight style) ---
    float rim = max(dot(normal, normalize(vec3(L3.x, 0.0, L3.z))), 0.0);
    rim = pow(rim, rimPower);

    // Tint rim by the light color so it doesn't get weird
    vec3 rimLight = rimColor * rim * rimIntensity * lightColor;

    // Attenuation
    float attenuation = 1.0 - (dist / lightRadius);
    attenuation = max(attenuation, 0.0);
    attenuation *= attenuation;

    // --- DIFFUSE ---
    float NdotL = max(dot(normal, L3), 0.0);
    float diffuse = NdotL * attenuation * lightIntensity;

    // --- BOUNCE ---
    float bounce = (1.0 - NdotL) * 0.4 * attenuation;

    // --- SPECULAR ---
    vec3 viewDir = normalize(vec3(0.0, -1.0, 0.0));
    vec3 reflectDir = reflect(-L3, normal);

    float spec = 0.0;
    if (NdotL > 0.0)
    {
        spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
    }

    vec3 specular = lightColor * spec * specularStrength;

    // --- FINAL LIGHT ---
    float totalLight = diffuse + bounce;

    // Apply ambient as a floor, not an add, so it doesn't stack per light
    totalLight = max(totalLight, ambient);

    vec3 lightContribution = lightColor * totalLight + specular; // + rimLight;

    // Write ONLY light
    finalColor = vec4(lightContribution, 1.0);
}