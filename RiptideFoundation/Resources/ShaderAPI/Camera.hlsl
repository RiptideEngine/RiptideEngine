#ifndef __INCLUDE_GUARD_RUNTIME_CAMERA__
#define __INCLUDE_GUARD_RUNTIME_CAMERA__

struct __CameraInformation_Underlying {
    float3 Position;
    uint Type;
    
    float4 ViewMatrix;
    float4 ProjectionMatrix;
    float4 ViewProjectionMatrix;
    
    float4 InvertViewMatrix;
    float4 InvertProjectionMatrix;
    float4 InvertViewProjectionMatrix;
};
ConstantBuffer<__CameraInformation_Underlying> _CameraInformation : register(b0, space32);

#endif