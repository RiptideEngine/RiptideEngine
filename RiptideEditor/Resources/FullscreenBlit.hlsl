#define ROOT_SIGNATURE \
    "StaticSampler(s0, space = 0, visibility = SHADER_VISIBILITY_PIXEL," \
        "addressU = TEXTURE_ADDRESS_CLAMP," \
        "addressV = TEXTURE_ADDRESS_CLAMP," \
        "addressW = TEXTURE_ADDRESS_CLAMP," \
        "filter = FILTER_MIN_MAG_MIP_POINT" \
    ")," \
    "DescriptorTable(SRV(t0, space = 0), visibility = SHADER_VISIBILITY_PIXEL),"

struct VSInput {
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct PSInput {
    float4 position : SV_Position;
    float2 uv : TexCoord;
};

// Vertex shader
[RootSignature(ROOT_SIGNATURE)]
PSInput vsmain(VSInput v) {
    PSInput o;
    
    o.uv = float2(uint2(v.vid, v.vid << 1) & 2);
    o.position = float4(lerp(float2(-1, 1), float2(1, -1), o.uv), 0, 1);
    
    return o;
};

Texture2D<float4> _MainTexture : register(t0, space0);
SamplerState _PointSampler : register(s0, space0);

// Pixel shader
[RootSignature(ROOT_SIGNATURE)]
float4 psmain(PSInput i) : SV_Target {
    return _MainTexture.Sample(_PointSampler, 1 - i.uv);
};