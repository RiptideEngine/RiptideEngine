#ifndef __INCLUDE_GUARD_RUNTIME_TRANSFORMATION__
#define __INCLUDE_GUARD_RUNTIME_TRANSFORMATION__

struct __Transformation_Underlying {
    float4x4 ModelMatrix;
    float4x4 InvertModelMatrix;
    
    float4x4 MVP;
    float4x4 InvertMVP;
};
ConstantBuffer<__Transformation_Underlying> _Transformation : register(b1, space32);

#endif