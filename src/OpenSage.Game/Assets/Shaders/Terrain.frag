#version 450
#extension GL_GOOGLE_include_directive : enable
#extension GL_EXT_samplerless_texture_functions : enable

#include "Common.h"
#include "Lighting.h"
#include "Cloud.h"
#include "Shadows.h"

layout(set = 0, binding = 0) uniform GlobalConstantsShared
{
    GlobalConstantsSharedType _GlobalConstantsShared;
};

layout(set = 0, binding = 4) uniform GlobalLightingConstantsPS
{
    GlobalLightingConstantsPSType _GlobalLightingConstantsPS;
};

layout(set = 0, binding = 5) uniform texture2D Global_CloudTexture;

layout(set = 0, binding = 6) uniform TerrainMaterialConstants
{
    vec2 MapBorderWidth;
    vec2 MapSize;
    bool IsMacroTextureStretched;
} _TerrainMaterialConstants;

layout(set = 0, binding = 7) uniform utexture2D TileData;

struct CliffInfo
{
    vec2 BottomLeftUV;
    vec2 BottomRightUV;
    vec2 TopRightUV;
    vec2 TopLeftUV;
};

layout(std430, set = 0, binding = 8) readonly buffer CliffDetails
{
    CliffInfo _CliffDetails[];
};

struct TextureInfo
{
    uint TextureIndex;
    uint CellSize;
    vec2 _Padding;
};

layout(std430, set = 0, binding = 9) readonly buffer TextureDetails
{
    TextureInfo _TextureDetails[];
};

layout(set = 0, binding = 10) uniform texture2DArray Textures;
layout(set = 0, binding = 11) uniform texture2D MacroTexture;
layout(set = 0, binding = 12) uniform sampler Sampler;

layout(set = 0, binding = 13) uniform ShadowConstantsPS
{
    ShadowConstantsPSType _ShadowConstantsPS;
};

layout(set = 0, binding = 14) uniform texture2DArray Global_ShadowMap;
layout(set = 0, binding = 15) uniform samplerShadow Global_ShadowSampler;

layout(location = 0) in vec3 in_WorldPosition;
layout(location = 1) in vec3 in_WorldNormal;
layout(location = 2) in vec2 in_UV;
layout(location = 3) in vec2 in_CloudUV;
layout(location = 4) in float in_ViewSpaceDepth;

in vec4 gl_FragCoord;

layout(location = 0) out vec4 out_Color;

vec3 SampleTexture(
    uint textureIndex,
    vec2 uv,
    vec2 ddxUV,
    vec2 ddyUV)
{
    // Can't do this because SPIRV-Cross doesn't support it yet:
    // TextureInfo textureInfo = _TextureDetails[textureIndex];

    uint textureDetailsCellSize = _TextureDetails[textureIndex].CellSize;
    uint textureDetailsIndex = _TextureDetails[textureIndex].TextureIndex;

    vec2 scaledUV = uv / textureDetailsCellSize;

    // Can't use standard Sample because UV is scaled by texture CellSize,
    // and that doesn't work for divergent texture lookups.
    vec4 diffuseTextureColor = textureGrad(
        sampler2DArray(Textures, Sampler),
        vec3(scaledUV, textureDetailsIndex),
        ddxUV / textureDetailsCellSize,
        ddyUV / textureDetailsCellSize);

    return diffuseTextureColor.xyz;
}

float CalculateDiagonalBlendFactor(vec2 fracUV, bool twoSided)
{
    float fracSum = fracUV.x + fracUV.y;
    return twoSided
        ? 1 - saturate(fracSum - 1)
        : saturate(1 - fracSum);
}

#define BLEND_DIRECTION_TOWARDS_RIGHT     1
#define BLEND_DIRECTION_TOWARDS_TOP       2
#define BLEND_DIRECTION_TOWARDS_TOP_RIGHT 4
#define BLEND_DIRECTION_TOWARDS_TOP_LEFT  8

float CalculateBlendFactor(
    uint blendDirection,
    uint blendFlags,
    vec2 fracUV)
{
    bool flipped = (blendFlags & 1) == 1;
    bool twoSided = (blendFlags & 2) == 2;

    if (flipped)
    {
        switch (blendDirection)
        {
            case BLEND_DIRECTION_TOWARDS_RIGHT:
                fracUV.x = 1 - fracUV.x;
                break;

            case BLEND_DIRECTION_TOWARDS_TOP:
            case BLEND_DIRECTION_TOWARDS_TOP_RIGHT:
            case BLEND_DIRECTION_TOWARDS_TOP_LEFT:
                fracUV.y = 1 - fracUV.y;
                break;
        }
    }

    float blendFactor = 0;

    switch (blendDirection)
    {
        case BLEND_DIRECTION_TOWARDS_RIGHT:
            blendFactor = fracUV.x;
            break;

        case BLEND_DIRECTION_TOWARDS_TOP:
            blendFactor = fracUV.y;
            break;

        case BLEND_DIRECTION_TOWARDS_TOP_RIGHT:
            fracUV = vec2(1, 1) - fracUV;
            blendFactor = CalculateDiagonalBlendFactor(fracUV, twoSided);
            break;

        case BLEND_DIRECTION_TOWARDS_TOP_LEFT:
            fracUV.y = 1 - fracUV.y;
            blendFactor = CalculateDiagonalBlendFactor(fracUV, twoSided);
            break;
    }

    return blendFactor;
}

vec3 SampleBlendedTextures(vec2 uv)
{
    uvec4 tileDatum = texelFetch(
        TileData,
        ivec2(uv),
        0);

    vec2 fracUV = fract(uv);

    uint cliffTextureIndex = tileDatum.z;
    if (cliffTextureIndex != 0)
    {
        // Can't do this because SPIRV-Cross doesn't support it yet:
        // CliffInfo cliffInfo = _CliffDetails[cliffTextureIndex - 1];

        vec2 cliffBottomLeftUV = _CliffDetails[cliffTextureIndex - 1].BottomLeftUV;
        vec2 cliffBottomRightUV = _CliffDetails[cliffTextureIndex - 1].BottomRightUV;
        vec2 cliffTopLeftUV = _CliffDetails[cliffTextureIndex - 1].TopLeftUV;
        vec2 cliffTopRightUV = _CliffDetails[cliffTextureIndex - 1].TopRightUV;

        vec2 uvXBottom = mix(cliffBottomLeftUV, cliffBottomRightUV, fracUV.x);
        vec2 uvXTop = mix(cliffTopLeftUV, cliffTopRightUV, fracUV.x);

        uv = mix(uvXBottom, uvXTop, fracUV.y);
    }

    vec2 ddxUV = dFdx(uv);
    vec2 ddyUV = dFdy(uv);

    uint packedTextureIndices = tileDatum.x;
    uint textureIndex0 = packedTextureIndices & 0xFFu;
    uint textureIndex1 = (packedTextureIndices >> 8) & 0xFFu;
    uint textureIndex2 = (packedTextureIndices >> 16) & 0xFFu;

    uint packedBlendInfo = tileDatum.y;
    uint blendDirection1 = packedBlendInfo & 0xFFu;
    uint blendFlags1 = (packedBlendInfo >> 8) & 0xFFu;
    uint blendDirection2 = (packedBlendInfo >> 16) & 0xFFu;
    uint blendFlags2 = (packedBlendInfo >> 24) & 0xFFu;

    vec3 textureColor0 = SampleTexture(textureIndex0, uv, ddxUV, ddyUV);
    vec3 textureColor1 = SampleTexture(textureIndex1, uv, ddxUV, ddyUV);
    vec3 textureColor2 = SampleTexture(textureIndex2, uv, ddxUV, ddyUV);

    float blendFactor1 = CalculateBlendFactor(blendDirection1, blendFlags1, fracUV);
    float blendFactor2 = CalculateBlendFactor(blendDirection2, blendFlags2, fracUV);

    return
        mix(
            mix(
                textureColor0,
                textureColor1,
                blendFactor1),
            textureColor2,
            blendFactor2);
}

vec2 GetMacroTextureUV(vec3 worldPosition)
{
    if (_TerrainMaterialConstants.IsMacroTextureStretched)
    {
        return 
            (worldPosition.xy + _TerrainMaterialConstants.MapBorderWidth) / 
            vec2(_TerrainMaterialConstants.MapSize.x, -_TerrainMaterialConstants.MapSize.y);
    }
    else
    {
        float macroTextureScale = 1 / 660.0f;
        return worldPosition.xy * vec2(macroTextureScale, -macroTextureScale);
    }
}

void main()
{
    float nDotL = saturate(dot(in_WorldNormal, -_GlobalLightingConstantsPS.Lights[0].Direction));
    vec3 shadowVisibility = ShadowVisibility(
        Global_ShadowMap,
        Global_ShadowSampler,
        in_WorldPosition, 
        in_ViewSpaceDepth, 
        nDotL, 
        in_WorldNormal, 
        ivec2(gl_FragCoord.xy), 
        _ShadowConstantsPS);

    vec3 diffuseColor;
    vec3 specularColor;

    DoLighting(
        _GlobalLightingConstantsPS,
        in_WorldPosition,
        in_WorldNormal,
        vec3(1, 1, 1),
        vec3(1, 1, 1),
        vec3(0, 0, 0),
        0,
        _GlobalConstantsShared.CameraPosition,
        false,
        shadowVisibility,
        diffuseColor,
        specularColor);

    vec3 textureColor = SampleBlendedTextures(in_UV);

    vec3 cloudColor = GetCloudColor(Global_CloudTexture, Sampler, in_CloudUV);

    vec2 macroTextureUV = GetMacroTextureUV(in_WorldPosition);
    vec3 macroTextureColor = texture(sampler2D(MacroTexture, Sampler), macroTextureUV).xyz;

    out_Color = vec4(
        diffuseColor * textureColor * cloudColor * macroTextureColor,
        1);
}