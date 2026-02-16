ivec2 spriteTexelCoord(int spriteIndex, int rowIndex)
{
    int linear = spriteIndex * u_RowsPerSprite + rowIndex;
    int x = linear % u_MaxSprites;
    int y = linear / u_MaxSprites;
    return ivec2(x, y);
}

vec4 readRow(sampler2D tex, int spriteIndex, int rowIndex)
{
    return texelFetch(tex, spriteTexelCoord(spriteIndex, rowIndex), 0);
}

float readFloat(sampler2D tex, int spriteIndex, int rowIndex)
{
    return readRow(tex, spriteIndex, rowIndex).r;
}

vec2 readVec2(sampler2D tex, int spriteIndex, int rowIndex)
{
    return readRow(tex, spriteIndex, rowIndex).rg;
}

vec3 readVec3(sampler2D tex, int spriteIndex, int rowIndex)
{
    return readRow(tex, spriteIndex, rowIndex).rgb;
}

vec4 readVec4(sampler2D tex, int spriteIndex, int rowIndex)
{
    return readRow(tex, spriteIndex, rowIndex);
}