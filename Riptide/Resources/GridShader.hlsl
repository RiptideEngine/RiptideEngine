#define ROOT_SIGNATURE \
    "RootFlags(DENY_HULL_SHADER_ROOT_ACCESS | DENY_DOMAIN_SHADER_ROOT_ACCESS | DENY_GEOMETRY_SHADER_ROOT_ACCESS | DENY_AMPLIFICATION_SHADER_ROOT_ACCESS | DENY_MESH_SHADER_ROOT_ACCESS)," \
    "CBV(b0, space = 0), " \
    "RootConstants(b1, space = 0, num32BitConstants = 5, visibility = SHADER_VISIBILITY_PIXEL),"

struct VSInput {
    uint vid : SV_VertexID;
};

struct PSInput {
    float4 sv_position : SV_Position;
    
    float3 near : NearPoint;
    float3 far : FarPoint;
};

cbuffer _Transformation : register(b0, space0) {
    float4x4 _ViewMatrix;
    float4x4 _InverseViewMatrix;
    
    float4x4 _ProjectionMatrix;
    float4x4 _InverseProjectionMatrix;
    
    float _PlaneNear;
    float _PlaneFar;
};

cbuffer _RootConstants : register(b1, space0) {
    float4 _GridColor = float4(1, 1, 1, 1);
    float _BaseLineAlpha = 0.5;
};

static const float4 _PlaneVertices[6] = {
    float4(-1, 1, 0, 1), float4(1, 1, 0, 1), float4(1, -1, 0, 1),
    float4(1, -1, 0, 1), float4(-1, -1, 0, 1), float4(-1, 1, 0, 1),
};

float3 UnprojectPoint(float x, float y, float z) {
    float4 unprojectedPoint = mul(_InverseViewMatrix, mul(_InverseProjectionMatrix, float4(x, y, z, 1.0f)));
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

[RootSignature(ROOT_SIGNATURE)]
PSInput vsmain(VSInput i) {
    PSInput o;
    
    float4 pt = _PlaneVertices[i.vid];
    
    o.near = UnprojectPoint(pt.x, pt.y, 0);
    o.far = UnprojectPoint(pt.x, pt.y, 1);
    o.sv_position = pt;
    
    return o;
}

float4 ComputeGrid(float3 pos, float scale) {
    float2 coord = pos.xz * scale;
    float2 derivative = fwidth(coord);
    float2 grid = abs(frac(coord - 0.5) - 0.5) / derivative;
    float lineValue = min(grid.x, grid.y);
    
    return 1 - min(lineValue, 1);
}

void ComputeDepth(float3 pos, float near, float far, out float depth, out float linearDepth) {
    float4 clip = mul(_ProjectionMatrix, mul(_ViewMatrix, float4(pos, 1)));
    depth = clip.z / clip.w;
    linearDepth = (near * far) / (far - depth * (far - near)) / far; // http://www.humus.name/temp/Linearize%20depth.txt
}

struct PSOutput {
    float4 color : SV_Target0;
    float depth : SV_Depth;
};

[RootSignature(ROOT_SIGNATURE)]
PSOutput psmain(PSInput i) {
    float t = -i.near.y / (i.far.y - i.near.y);
    
    float3 pos = i.near + t * (i.far - i.near);
    
    float depth = 0;
    float linearDepth = 0;
    ComputeDepth(pos, _PlaneNear, _PlaneFar, depth, linearDepth);
    
    PSOutput o;
    o.color = saturate(ComputeGrid(pos, 1)) * _GridColor * float(t > 0);
    o.color.a *= saturate(_BaseLineAlpha - linearDepth);
    
    o.depth = depth;
    
    return o;
}