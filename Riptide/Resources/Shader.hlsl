#define ROOT_SIGNATURE_COMMON

#define ROOT_SIGNATURE \
    "CBV(b0, space = 0)," \
    "SRV(t0, space = 0, visibility = SHADER_VISIBILITY_VERTEX)," \
    "RootConstants(b1, space = 0, num32BitConstants = 4, visibility = SHADER_VISIBILITY_PIXEL)," \

struct Vertex {
    float3 pos;
};

struct VertexShaderInput {
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct PixelShaderInput {
    float4 sv_pos : SV_Position;
};

StructuredBuffer<Vertex> _VertexBuffer : register(t0, space0);

cbuffer _Transformation : register(b0, space0) {
    float4x4 _MVP;
};

cbuffer _RootConstants : register(b1, space0) {
    float4 _Color;
}

[RootSignature(ROOT_SIGNATURE)]
PixelShaderInput vsmain(VertexShaderInput i) {
    PixelShaderInput o;
    
    Vertex v = _VertexBuffer[i.vid];
    
    o.sv_pos = mul(_MVP, float4(v.pos, 1));
    
    return o;
};

[RootSignature(ROOT_SIGNATURE)]
float4 psmain(PixelShaderInput i) : SV_Target {
    return _Color;
};
