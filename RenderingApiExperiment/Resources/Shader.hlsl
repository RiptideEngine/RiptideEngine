struct Vertex {
    float3 position;
    uint color;
    float2 uv;
};

struct PSInput {
    float4 sv_position : SV_Position;
    float4 color : Color;
    float2 uv : TexCoord;
};

StructuredBuffer<Vertex> _Vertices : register(t0, space0);

struct __Transformation_UnderlyingStruct {
    float4x4 mvp;
};
ConstantBuffer<__Transformation_UnderlyingStruct> _Transformation : register(b0, space0);

PSInput vsmain(const uint vid : SV_VertexID) {
    PSInput o;
    Vertex v = _Vertices[vid];

    o.sv_position = mul(_Transformation.mvp, float4(v.position, 1));
    o.color = float4(v.color & 0xFF, v.color >> 8 & 0xFF, v.color >> 16 & 0xFF, v.color >> 24) / 255;
    o.uv = v.uv;
    
    return o;
}

Texture2D _MainTexture : register(t0, space1);
SamplerState _Sampler : register(s0, space1);

float4 psmain(const PSInput i) : SV_Target0 {
    return i.color * _MainTexture.Sample(_Sampler, i.uv);
}