struct Vertex {
    float2 position;
    float2 uv;
    uint color;
};

struct Fragment {
    float4 position : SV_Position;
    float2 uv : TexCoord;
    float4 color : Color;
};

StructuredBuffer<Vertex> _Vertices : register(t0, space0);

cbuffer _Transformation : register(b0, space0) {
    float4x4 _MVP;
};

Fragment vsmain(const uint vid : SV_VertexID) {
    Vertex i = _Vertices[vid];
    Fragment o;
    
    o.position = mul(_MVP, float4(i.position, 0, 1));
    o.uv = i.uv;
    o.color = float4(i.color & 0xFF, i.color >> 8 & 0xFF, i.color >> 16 & 0xFF, i.color >> 24) / 255.0f;
    
    return o;
};

Texture2D<float> _MainTexture : register(t0, space1);
SamplerState _Sampler : register(s0, space1);

float4 psmain(const Fragment i) : SV_Target {
    float4 color = i.color;
    float distance = 0.5 - _MainTexture.Sample(_Sampler, i.uv);
    float2 ddist = float2(ddx(distance), ddy(distance));
    
    float pixelDist = distance / length(ddist);
    
    color.a = saturate(0.5 - pixelDist);

    return color;
};