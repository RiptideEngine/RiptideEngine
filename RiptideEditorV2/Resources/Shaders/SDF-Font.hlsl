struct Vertex {
    float2 position;
    float2 uv0;
    uint color;
};

struct Fragment {
    float4 position : SV_Position;
    float2 uv0 : TexCoord0;
    float4 color : Color;
};

StructuredBuffer<Vertex> _Vertices : register(t0, space0);

cbuffer _Transformation : register(b0, space0) {
    float4x4 _Perspective;
}

Fragment vsmain(const uint vid : SV_VertexID) {
    Vertex i = _Vertices[vid];
    Fragment o;
    
    o.position = mul(_Perspective, float4(i.position, 0, 1));
    o.uv0 = i.uv0;
    o.color = float4(i.color & 0xFF, i.color >> 8 & 0xFF, i.color >> 16 & 0xFF, i.color >> 24) / 255.0f;
    
    return o;
}

Texture2D<float> _FontBitmap : register(t0, space1);

SamplerState _Sampler : register(s0, space1);

float4 psmain(const Fragment i) : SV_Target {
    float4 color = i.color;
    float distance = 0.5 - _FontBitmap.Sample(_Sampler, i.uv0);
    float2 ddist = float2(ddx(distance), ddy(distance));
    
    float pixelDist = distance / length(ddist);
    
    color.a = saturate(0.5 - pixelDist);
    
    return color;
}