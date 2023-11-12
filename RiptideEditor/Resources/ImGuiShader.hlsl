struct Vertex {
    float2 position;
    float2 uv;
    uint color;
};

struct PSInput {
    float4 sv_position : SV_Position;
    float4 color : Color;
    float2 uv : TexCoord;
};

StructuredBuffer<Vertex> _VertexBuffer : register(t0, space0);

cbuffer _RootConstants : register(b0, space0) {
    float4x4 _ProjectionMatrix;
    uint _BaseVertexLocation;
};

float4 UnpackColor(uint color) {
    return float4(color & 0xFF, (color & 0x0000FF00) >> 8, (color & 0x00FF0000) >> 16, color >> 24) / 255.0;
}

PSInput vsmain(uint vid : SV_VertexID) {
    PSInput o;
    Vertex v = _VertexBuffer[vid + _BaseVertexLocation];
    
    o.sv_position = mul(_ProjectionMatrix, float4(v.position, 0, 1));
    o.uv = v.uv;
    o.color = UnpackColor(v.color);
    
    return o;
};

Texture2D<float4> _MainTexture : register(t1, space0);
SamplerState _Sampler : register(s0, space0);

float4 psmain(PSInput i) : SV_Target {
    return _MainTexture.Sample(_Sampler, i.uv) * i.color;
};