#include "ShaderAPI/Vertex.hlsl"

#define ROOT_SIGNATURE_COMMON

#define ROOT_SIGNATURE \
    "CBV(b0, space = 0)," \
    "SRV(" RIPTIDE_VERTEX_ROOTSIGNATURE_LOCATION(0) ", visibility = SHADER_VISIBILITY_VERTEX)," \

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

RIPTILE_DECLARE_VERTEX_BUFFER(0, Vertex);

cbuffer _Transformation : register(b0, space0) {
    float4x4 _Model;
    float4x4 _MVP;
};

[RootSignature(ROOT_SIGNATURE)]
PixelShaderInput vsmain(VertexShaderInput i) {
    PixelShaderInput o;
    
    Vertex v = GET_VERTEX_DATA(0, i.vid);
    
    o.sv_pos = mul(_MVP, float4(v.pos, 1));
    
    return o;
};

[RootSignature(ROOT_SIGNATURE)]
float4 psmain(PixelShaderInput i) : SV_Target {
    return float4(1, 1, 1, 1);
};
