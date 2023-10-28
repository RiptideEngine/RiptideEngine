namespace RiptideRendering.Direct3D12;

internal static unsafe class D3D12Utils {
    public static void GetIndicesOfRootDescriptors(RootParameter* pParameters, uint numParameters, D3D12ShaderReflector reflector, IList<uint> indices) {
        for (uint i = 0; i < numParameters; i++) {
            ref readonly var parameter = ref pParameters[i];

            switch (parameter.ParameterType) {
                case RootParameterType.TypeCbv: {
                    ref readonly var descriptor = ref parameter.Descriptor;

                    if (reflector.HasConstantBuffer(new ResourceBindLocation(descriptor.ShaderRegister, descriptor.RegisterSpace))) {
                        indices.Add(i);
                    }
                    break;
                }

                case RootParameterType.TypeSrv: {
                    ref readonly var descriptor = ref parameter.Descriptor;

                    if (reflector.HasReadonlyResource(new ResourceBindLocation(descriptor.ShaderRegister, descriptor.RegisterSpace))) {
                        indices.Add(i);
                    }
                    break;
                }

                case RootParameterType.TypeUav: {
                    ref readonly var descriptor = ref parameter.Descriptor;

                    if (reflector.HasReadWriteResource(new ResourceBindLocation(descriptor.ShaderRegister, descriptor.RegisterSpace))) {
                        indices.Add(i);
                    }
                    break;
                }
            }
        }
    }

    public static bool CheckIsRootConstant(RootParameter* pParameters, uint numParameters, uint register, uint space) {
        for (uint i = 0; i < numParameters; i++) {
            ref readonly var parameter = ref pParameters[i];

            if (parameter.ParameterType != RootParameterType.Type32BitConstants) continue;

            ref readonly var rootConst = ref parameter.Constants;

            if (rootConst.ShaderRegister != register) continue;
            if (rootConst.RegisterSpace != space) continue;

            return true;
        }

        return false;
    }

    public static void CountTotalDescriptors(DescriptorRange* pRanges, uint numRanges, out uint numResourceDescriptors, out uint numSamplerDescriptors) {
        numResourceDescriptors = 0;
        numSamplerDescriptors = 0;

        for (uint i = 0; i < numRanges; i++) {
            ref readonly var range = ref pRanges[i];

            if (range.RangeType == DescriptorRangeType.Sampler) {
                numSamplerDescriptors += range.NumDescriptors;
            } else {
                numResourceDescriptors += range.NumDescriptors;
            }
        }
    }

    public static void CountTotalDescriptors(RootParameter* pParameters, uint numParameters, out uint numResourceDescriptors, out uint numSamplerDescriptors) {
        numResourceDescriptors = 0;
        numSamplerDescriptors = 0;

        for (uint i = 0; i < numParameters; i++) {
            ref readonly var parameter = ref pParameters[i];

            if (parameter.ParameterType != RootParameterType.TypeDescriptorTable) continue;

            ref readonly var table = ref parameter.DescriptorTable;

            for (uint r = 0; r < table.NumDescriptorRanges; r++) {
                ref readonly var range = ref table.PDescriptorRanges[r];

                if (range.RangeType == DescriptorRangeType.Sampler) {
                    numSamplerDescriptors += range.NumDescriptors;
                } else {
                    numResourceDescriptors += range.NumDescriptors;
                }
            }
        }
    }
}