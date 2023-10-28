#define ROOT_SIGNATURE \
    "CBV(b0, space = 0)," \
    "UAV(u0, space = 0)," \

cbuffer _Time : register(b0, space0) {
    float _ElapsedTime;
};

RWStructuredBuffer<float4x4> _OutputTransformations : register(u0, space0);

[RootSignature(ROOT_SIGNATURE)]
[numthreads(16, 1, 1)]
void csmain(uint3 id : SV_DispatchThreadID) {
    float x = _ElapsedTime + id.x * (3.14159265 * 2) / 32;
    
    _OutputTransformations[id.x] = float4x4(1, 0, 0, cos(x) * 4, 0, 1, 0, sin(x) * 4, 0, 0, 1, 0, 0, 0, 0, 1);
}