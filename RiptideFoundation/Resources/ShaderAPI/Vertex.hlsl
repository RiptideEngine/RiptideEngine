#ifndef RIPTIDE_INCLGUARD_VERTEX
#define RIPTIDE_INCLGUARD_VERTEX

#include "CompilerUtils.hlsl"

#define RIPTIDE_VERTEXBUFFER_SPACE                      1024
#define GET_VERTEXBUFFER_NAME(channel)                  COMBINE_2_TOKENS(_RIPTIDE_VERTEXBUFFER_C, channel)

#define RIPTIDE_VERTEX_ROOTSIGNATURE_LOCATION(channel)  STRINGIFY(COMBINE_2_TOKENS(t, channel)) ", space = " STRINGIFY(RIPTIDE_VERTEXBUFFER_SPACE)

#define RIPTILE_DECLARE_VERTEX_BUFFER(channel, type)    StructuredBuffer<type> GET_VERTEXBUFFER_NAME(channel) : register(COMBINE_2_TOKENS(t, channel), COMBINE_2_TOKENS(space, RIPTIDE_VERTEXBUFFER_SPACE))

#define GET_VERTEX_DATA(channel, index)                 GET_VERTEXBUFFER_NAME(channel)[index]

#endif  // RIPTIDE_INCLGUARD_VERTEX