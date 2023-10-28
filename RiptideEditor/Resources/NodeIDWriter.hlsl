#define ROOT_SIGNATURE \
	"SRV(t0, space = 0, visibility = SHADER_VISIBILITY_VERTEX)," \
	"CBV(b0, space = 0, visibility = SHADER_VISIBILITY_VERTEX)," \
	"RootConstants(b1, space = 0, num32BitConstants = 1, visibility = SHADER_VISIBILITY_PIXEL),"

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

cbuffer _Transformation : register(b0, space0) {
	float4x4 _MVP;
};
cbuffer _RootConstants : register(b1, space0) {
	uint _ID;
};

StructuredBuffer<Vertex> _VertexPositions : register(t0, space0);

[RootSignature(ROOT_SIGNATURE)]
PixelShaderInput vsmain(VertexShaderInput i) {
	Vertex v = _VertexPositions[i.vid];
	
	PixelShaderInput o;
	o.sv_pos = mul(_MVP, float4(v.pos, 1));
	
	return o;
}

[RootSignature(ROOT_SIGNATURE)]
uint psmain(PixelShaderInput i) : SV_Target0 {
	return _ID;
}