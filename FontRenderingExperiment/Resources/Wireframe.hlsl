struct Vertex {
    float2 position;
    float2 uv;
    uint color;
};

struct Fragment {
    float4 position : SV_Position;
};

StructuredBuffer<Vertex> _Vertices : register(t0, space0);

cbuffer _Transformation : register(b0, space0) {
    float4x4 _MVP;
};

Fragment vsmain(const uint vid : SV_VertexID) {
    Vertex i = _Vertices[vid];
    Fragment o;
    
    o.position = mul(_MVP, float4(i.position, 0, 1));
    
    return o;
};

float4 psmain(const Fragment i) : SV_Target {
    return float4(0, 0, 0, 1);
};