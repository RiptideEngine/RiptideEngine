struct Vertex {
    float2 position;
    float2 uv;
    uint color;
};

struct PSInput {
    float4 position : SV_Position;
    float2 uv : TexCoord0;
    float4 color : Color;
};

StructuredBuffer<Vertex> _Vertices : register(t0, space0);

cbuffer _Transformation : register(b0, space0) {
    float4x4 _Perspective;
};

PSInput vsmain(const uint vid : SV_VertexID) {
    PSInput o;
    Vertex v = _Vertices[vid];
    
    o.position = mul(_Perspective, float4(v.position, 0, 1));
    o.uv = v.uv;
    o.color = float4(v.color & 0xFF, v.color >> 8 & 0xFF, v.color >> 16 & 0xFF, v.color >> 24) / 255.0;
    
    return o;
}

Texture2D _MainTexture : register(t0, space1);
SamplerState _Sampler : register(s0, space1);

float4 psmain(PSInput i) : SV_Target {
    return i.color * _MainTexture.Sample(_Sampler, i.uv);
}