namespace RiptideRendering.Direct3D12;

internal static unsafe class D3D12Helper {
    public const uint DefaultShader4ComponentMapping = 0 | 1 << 3 | 2 << 6 | 3 << 9 | 1 << 12;

    public static CpuDescriptorHandle UnknownCpuHandle => Unsafe.BitCast<nuint, CpuDescriptorHandle>(nuint.MaxValue);
    public static GpuDescriptorHandle UnknownGpuHandle => Unsafe.BitCast<ulong, GpuDescriptorHandle>(ulong.MaxValue);

    public static ResourceBarrier TransitionResource(ID3D12Resource* pResource, D3D12ResourceStates before, D3D12ResourceStates after, uint subresource = uint.MaxValue, ResourceBarrierFlags flags = ResourceBarrierFlags.None) {
        return new() {
            Type = ResourceBarrierType.Transition,
            Flags = flags,
            Transition = new() {
                PResource = pResource,
                StateBefore = before,
                StateAfter = after,
                Subresource = subresource,
            },
        };
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint EncodeShader4ComponentMapping(ShaderComponentMapping x, ShaderComponentMapping y, ShaderComponentMapping z, ShaderComponentMapping w) {
        return (uint)x | (uint)y << 3 | (uint)z << 6 | (uint)w << 9 | 1 << 12;
    }

    public static void MemcpySubresource(MemcpyDest* pDest, SubresourceData* pSrc, ulong rowSizeInBytes, uint numRows, uint numSlices) {
        for (uint z = 0; z < numSlices; z++) {
            var pDestSlice = (byte*)pDest->PData + pDest->SlicePitch * z;
            var pSrcSlice = (byte*)pSrc->PData + pSrc->SlicePitch * z;

            for (uint y = 0; y < numRows; y++) {
                Unsafe.CopyBlock(pDestSlice + pDest->RowPitch * y, pSrcSlice + pSrc->RowPitch * y, (uint)rowSizeInBytes);
            }
        }
    }

    public static void SetName<T>(T* pObject, ReadOnlySpan<char> name) where T : unmanaged, IComVtbl<ID3D12Object> {
        fixed (char* pName = name) {
            var guid = new Guid(0x4cca5fd8, 0x921f, 0x42c8, 0x85, 0x66, 0x70, 0xca, 0xf2, 0xa9, 0xb7, 0x41);

            ((ID3D12Object*)pObject)->SetPrivateData(&guid, sizeof(char) * (uint)name.Length, pName);
        }
    }

    public static bool TryFindTextureBindingLocation(uint register, uint space, RootParameter* pParameters, uint numParameters, DescriptorRangeType descriptorRangeType, out uint parameterIndex, out uint descriptorOffset) {
        for (uint i = 0; i < numParameters; i++) {
            ref readonly var param = ref pParameters[i];

            if (param.ParameterType != RootParameterType.TypeDescriptorTable) continue;

            ref readonly var table = ref param.DescriptorTable;

            for (uint r = 0; r < table.NumDescriptorRanges; r++) {
                ref readonly var range = ref table.PDescriptorRanges[r];

                if (range.RangeType != descriptorRangeType) continue;
                if (range.RegisterSpace != space) continue;

                if (range.BaseShaderRegister > register || register >= range.BaseShaderRegister + range.NumDescriptors) continue;

                parameterIndex = i;
                descriptorOffset = register - range.BaseShaderRegister;

                return true;
            }
        }

        parameterIndex = descriptorOffset = uint.MaxValue;
        return false;
    }

    public static ShaderBytecode CreateShaderBytecode(IDxcBlob* pBlob) => new() { PShaderBytecode = pBlob->GetBufferPointer(), BytecodeLength = pBlob->GetBufferSize() };
}