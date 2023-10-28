namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12CommandList {
    private readonly List<uint> _unboundedGraphicsRootDescriptorIndices = new();
    private readonly List<uint> _unboundedComputeRootDescriptorIndices = new();

    private void EnsureAllGraphicsRootDescriptorsBounded() {
        if (_unboundedGraphicsRootDescriptorIndices.Count == 0) return;

        pCommandList.Close();
        pCommandList.Reset(pAllocator, (ID3D12PipelineState*)null);

        _graphicalShader?.DecrementReference(); _graphicalShader = null;

        throw new InvalidOperationException($"Direct3D12 requires all graphics root descriptor must be bounded into the CommandList before pushing draw call. Unbounded root parameter indices: [{string.Join(", ", _unboundedGraphicsRootDescriptorIndices)}]");
    }

    private void EnsureAllComputeRootDescriptorBounded() {
        if (_unboundedComputeRootDescriptorIndices.Count == 0) return;

        pCommandList.Close();
        pCommandList.Reset(pAllocator, (ID3D12PipelineState*)null);

        _computeShader?.DecrementReference(); _computeShader = null;

        throw new InvalidOperationException($"Direct3D12 requires all compute root descriptor must be bounded into the CommandList before pushing compute call. Unbounded root parameter indices: [{string.Join(", ", _unboundedComputeRootDescriptorIndices)}]");
    }

    public override void SetPipelineState(PipelineState state) {
        if (state is not D3D12PipelineState d3d12pso) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "PipelineState", "Direct3D12's PipelineState"), nameof(state));

        EnsureNotClosed();

        switch (state.Type) {
            case PipelineStateType.Graphical:
                var d3d12gshader = Unsafe.As<D3D12GraphicalShader>(d3d12pso.Shader);

                pCommandList.IASetPrimitiveTopology(d3d12gshader.HasTessellationStages ? D3DPrimitiveTopology.D3DPrimitiveTopology3ControlPointPatchlist : D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
                break;
        }

        pCommandList.SetPipelineState(d3d12pso.PipelineState);

        _descCommitter.FinalizeResources();
    }
    public override void SetIndexBuffer(NativeBufferHandle resource, IndexFormat format, uint offset) {
        EnsureNotClosed();

        if (resource.Handle == 0) {
            pCommandList.IASetIndexBuffer((IndexBufferView*)null);
        } else {
            var d3d12resource = ((ID3D12Resource*)resource.Handle);
            var rdesc = d3d12resource->GetDesc();

            if (offset >= rdesc.Width) {
                pCommandList.IASetIndexBuffer((IndexBufferView*)null);
            } else {
                if (!format.IsDefined()) throw new ArgumentException("Undefined format enumeration.");

                IndexBufferView view = new() {
                    BufferLocation = ((ID3D12Resource*)resource.Handle)->GetGPUVirtualAddress() + offset,
                    SizeInBytes = (uint)(rdesc.Width - offset),
                    Format = (Format)((uint)Format.FormatR16Uint - 15 * (uint)format),
                };

                pCommandList.IASetIndexBuffer(&view);
            }
        }
    }

    public override void SetGraphicsBindingSchematic(GraphicalShader shader) {
        if (shader is not D3D12GraphicalShader d3d12shader) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GraphicalShader", "Direct3D12's GraphicalShader"), nameof(shader));

        EnsureNotClosed();

        _unboundedGraphicsRootDescriptorIndices.Clear();

        pCommandList.SetGraphicsRootSignature(d3d12shader.RootSignature);

        _graphicalShader?.DecrementReference();
        _graphicalShader = d3d12shader;
        d3d12shader.IncrementReference();

        ref readonly RootSignatureDesc rsdesc = ref d3d12shader.GetRootSignatureDesc()->Desc10;
        var reflector = Unsafe.As<D3D12ShaderReflector>(d3d12shader.Reflector);

        D3D12Utils.GetIndicesOfRootDescriptors(rsdesc.PParameters, rsdesc.NumParameters, reflector, _unboundedGraphicsRootDescriptorIndices);

        _descCommitter.ResetGraphics();
        _descCommitter.InitializeGraphicsBindingSchematic(rsdesc.PParameters, rsdesc.NumParameters, reflector);
    }

    //public override void SetComputeBindingSchematic(ComputeShader shader) {
    //    if (shader is not D3D12ComputeShader d3d12shader) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "ComputeShader", "Direct3D12's ComputeShader"), nameof(shader));

    //    _unboundedComputeRootDescriptorIndices.Clear();

    //    pCommandList.SetComputeRootSignature(d3d12shader.RootSignature);

    //    _computeShader?.DecrementReference();
    //    _computeShader = d3d12shader;
    //    d3d12shader.IncrementReference();

    //    ref readonly RootSignatureDesc rsdesc = ref d3d12shader.GetRootSignatureDesc()->Desc10;
    //    var reflector = Unsafe.As<D3D12ShaderReflector>(d3d12shader.Reflector);

    //    _descCommitter.ResetCompute();
    //    D3D12Utils.GetIndicesOfRootDescriptors(rsdesc.PParameters, rsdesc.NumParameters, reflector, _unboundedComputeRootDescriptorIndices);
    //    _descCommitter.InitializeComputeBindingSchematic(rsdesc.PParameters, rsdesc.NumParameters, reflector);
    //}

    public override unsafe void SetGraphicsDynamicConstantBuffer(ReadOnlySpan<char> name, ReadOnlySpan<byte> data) {
        if (_graphicalShader == null || data.IsEmpty) return;

        EnsureNotClosed();

        var reflector = Unsafe.As<D3D12ShaderReflector>(_graphicalShader.Reflector);
        foreach (ref readonly var rinfo in reflector.ConstantBufferInfos.AsSpan()) {
            if (!name.SequenceEqual(rinfo.Name)) continue;

            ref readonly var rsdesc = ref _graphicalShader.GetRootSignatureDesc()->Desc10;

            var rootParams = rsdesc.PParameters;
            var numRootParams = rsdesc.NumParameters;

            for (uint i = 0; i < numRootParams; i++) {
                ref readonly var param = ref rootParams[i];

                switch (param.ParameterType) {
                    case RootParameterType.Type32BitConstants:
                        ref readonly var consts = ref param.Constants;

                        if (consts.ShaderRegister != rinfo.Location.Register) continue;
                        if (consts.RegisterSpace != rinfo.Location.Space) continue;

                        pCommandList.SetGraphicsRoot32BitConstants(i, (uint)data.Length / sizeof(uint), data, 0);
                        return;

                    case RootParameterType.TypeCbv: {
                        ref readonly var desc = ref param.Descriptor;

                        if (desc.ShaderRegister != rinfo.Location.Register) continue;
                        if (desc.RegisterSpace != rinfo.Location.Space) continue;

                        uint dataLength = uint.Min((uint)data.Length, 65_536); // 65536 bytes is the maximum size of constant buffer in D3D12.

                        _unboundedGraphicsRootDescriptorIndices.Remove(i);
                        var allocation = _uploadBuffer.Allocate(dataLength, D3D12.ConstantBufferDataPlacementAlignment);

                        Unsafe.CopyBlock(allocation.CpuAddress, Unsafe.AsPointer(ref MemoryMarshal.GetReference(data)), dataLength);

                        pCommandList.SetGraphicsRootConstantBufferView(i, allocation.VirtualAddress);
                        return;
                    }

                    case RootParameterType.TypeDescriptorTable: {
                        ref readonly var table = ref param.DescriptorTable;

                        for (uint r = 0; r < table.NumDescriptorRanges; r++) {
                            ref readonly var range = ref table.PDescriptorRanges[r];

                            if (range.RangeType != DescriptorRangeType.Cbv) continue;
                            if (range.RegisterSpace != rinfo.Location.Space) continue;
                            if (range.BaseShaderRegister > rinfo.Location.Register || rinfo.Location.Register >= range.BaseShaderRegister + range.NumDescriptors) continue;

                            uint allocateAmount = MathUtils.AlignUpwardPow2(uint.Max((uint)data.Length, 65_536), 256U);

                            var allocation = _uploadBuffer.Allocate(allocateAmount, D3D12.ConstantBufferDataPlacementAlignment);
                            Unsafe.CopyBlock(allocation.CpuAddress, Unsafe.AsPointer(ref MemoryMarshal.GetReference(data)), (uint)data.Length);

                            var handle = _descCommitter.GetGraphicsResourceDescriptor(i, rinfo.Location.Register - range.BaseShaderRegister);

                            _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
                                BufferLocation = allocation.VirtualAddress,
                                SizeInBytes = allocateAmount,
                            }, handle);

                            return;
                        }
                        break;
                    }
                }
            }

            break;
        }
    }

    //public override void SetGraphicsReadonlyResource(ReadOnlySpan<char> name, NativeResourceHandle resource) {
    //    if (_graphicalShader == null) return;

    //    var reflector = Unsafe.As<D3D12ShaderReflector>(_graphicalShader.Reflector);
    //    foreach (ref readonly var rinfo in reflector.ReadonlyResourceInfos.AsSpan()) {
    //        if (!name.SequenceEqual(rinfo.Name)) continue;
    //        if (!rinfo.Type.IsReadonlyBuffer()) continue;

    //        ref readonly var rsdesc = ref _graphicalShader.GetRootSignatureDesc()->Desc10;

    //        var rootParams = rsdesc.PParameters;
    //        var numRootParams = rsdesc.NumParameters;

    //        if (resource.Handle == 0) {
    //            for (uint p = 0; p < numRootParams; p++) {
    //                ref readonly var param = ref rootParams[p];

    //                switch (param.ParameterType) {
    //                    case RootParameterType.TypeCbv: {
    //                        ref readonly var descriptor = ref param.Descriptor;

    //                        if (descriptor.RegisterSpace != rinfo.Location.Space) continue;
    //                        if (descriptor.ShaderRegister != rinfo.Location.Register) continue;
    //                        if (rinfo.Type != ResourceType.ConstantBuffer) continue;

    //                        if (!_unboundedGraphicsRootDescriptorIndices.Contains(p)) {
    //                            _unboundedGraphicsRootDescriptorIndices.Add(p);
    //                        }
    //                        break;
    //                    }

    //                    case RootParameterType.TypeSrv: {
    //                        ref readonly var descriptor = ref param.Descriptor;

    //                        if (descriptor.RegisterSpace != rinfo.Location.Space) continue;
    //                        if (descriptor.ShaderRegister != rinfo.Location.Register) continue;
    //                        if (!rinfo.Type.IsReadonly()) continue;

    //                        if (!_unboundedGraphicsRootDescriptorIndices.Contains(p)) {
    //                            _unboundedGraphicsRootDescriptorIndices.Add(p);
    //                        }
    //                        break;
    //                    }

    //                    case RootParameterType.TypeDescriptorTable: {
    //                        ref readonly var table = ref param.DescriptorTable;

    //                        for (uint r = 0; r < table.NumDescriptorRanges; r++) {
    //                            ref readonly var range = ref table.PDescriptorRanges[r];

    //                            if (range.RegisterSpace != rinfo.Location.Space) continue;
    //                            if (range.BaseShaderRegister > rinfo.Location.Register || rinfo.Location.Register >= range.BaseShaderRegister + range.NumDescriptors) continue;

    //                            var handle = _descCommitter.GetGraphicsResourceDescriptor(p, rinfo.Location.Register - range.BaseShaderRegister);

    //                            switch (range.RangeType) {
    //                                case DescriptorRangeType.Cbv:
    //                                    _context.Device->CreateConstantBufferView(null, handle);
    //                                    break;

    //                                case DescriptorRangeType.Srv:
    //                                    ShaderResourceViewDesc desc = new() {
    //                                        Shader4ComponentMapping = D3D12Helper.DefaultShader4ComponentMapping,
    //                                    };

    //                                    switch (rinfo.Type) {
    //                                        case ResourceType.TextureBuffer:
    //                                            desc.Format = Format.FormatR32Float;
    //                                            desc.ViewDimension = SrvDimension.Buffer;
    //                                            break;

    //                                        case ResourceType.Buffer:
    //                                            desc.Format = Format.FormatR32Float;
    //                                            desc.ViewDimension = SrvDimension.Buffer;
    //                                            break;

    //                                        case ResourceType.StructuredBuffer:
    //                                            desc.Format = Format.FormatUnknown;
    //                                            desc.ViewDimension = SrvDimension.Buffer;
    //                                            break;

    //                                        case ResourceType.ByteAddressBuffer:
    //                                            desc.Format = Format.FormatR32Uint;
    //                                            desc.ViewDimension = SrvDimension.Buffer;
    //                                            desc.Buffer.Flags = BufferSrvFlags.Raw;
    //                                            break;

    //                                        case ResourceType.Texture1D:
    //                                            desc.Format = Format.FormatR8Uint;
    //                                            desc.ViewDimension = SrvDimension.Texture1D;
    //                                            break;

    //                                        case ResourceType.Texture1DArray:
    //                                            desc.Format = Format.FormatR8Uint;
    //                                            desc.ViewDimension = SrvDimension.Texture1Darray;
    //                                            break;

    //                                        case ResourceType.Texture2D:
    //                                            desc.Format = Format.FormatR8Uint;
    //                                            desc.ViewDimension = SrvDimension.Texture2D;
    //                                            break;

    //                                        case ResourceType.Texture2DArray:
    //                                            desc.Format = Format.FormatR8Uint;
    //                                            desc.ViewDimension = SrvDimension.Texture2Darray;
    //                                            break;

    //                                        case ResourceType.Texture2DMS:
    //                                            desc.Format = Format.FormatR8Uint;
    //                                            desc.ViewDimension = SrvDimension.Texture2Dms;
    //                                            break;

    //                                        case ResourceType.Texture2DMSArray:
    //                                            desc.Format = Format.FormatR8Uint;
    //                                            desc.ViewDimension = SrvDimension.Texture2Dmsarray;
    //                                            break;

    //                                        case ResourceType.Texture3D:
    //                                            desc.Format = Format.FormatR8Unorm;
    //                                            desc.ViewDimension = SrvDimension.Texture3D;
    //                                            break;

    //                                        case ResourceType.TextureCube:
    //                                            desc.Format = Format.FormatR8Uint;
    //                                            desc.ViewDimension = SrvDimension.Texturecube;
    //                                            break;

    //                                        case ResourceType.TextureCubeArray:
    //                                            desc.Format = Format.FormatR8Uint;
    //                                            desc.ViewDimension = SrvDimension.Texturecubearray;
    //                                            break;
    //                                    }

    //                                    _context.Device->CreateShaderResourceView(null, &desc, handle);
    //                                    break;
    //                            }
    //                        }

    //                        break;
    //                    }
    //                }
    //            }
    //        } else {
    //            var d3d12resource = (ID3D12Resource*)resource.Handle;
    //            var rdesc = d3d12resource->GetDesc();

    //            for (uint p = 0; p < numRootParams; p++) {
    //                ref readonly var param = ref rootParams[p];

    //                switch (param.ParameterType) {
    //                    case RootParameterType.TypeCbv: {
    //                        ref readonly var descriptor = ref param.Descriptor;

    //                        if (descriptor.RegisterSpace != rinfo.Location.Space) continue;
    //                        if (descriptor.ShaderRegister != rinfo.Location.Register) continue;
    //                        if (rinfo.Type != ResourceType.ConstantBuffer) continue;

    //                        _unboundedGraphicsRootDescriptorIndices.Remove(p);
    //                        pCommandList.SetGraphicsRootConstantBufferView(p, ((ID3D12Resource*)resource.Handle)->GetGPUVirtualAddress());
    //                        return;
    //                    }

    //                    case RootParameterType.TypeSrv: {
    //                        ref readonly var descriptor = ref param.Descriptor;

    //                        if (descriptor.RegisterSpace != rinfo.Location.Space) continue;
    //                        if (descriptor.ShaderRegister != rinfo.Location.Register) continue;
    //                        if (!rinfo.Type.IsReadonly()) continue;

    //                        _unboundedGraphicsRootDescriptorIndices.Remove(p);
    //                        pCommandList.SetGraphicsRootShaderResourceView(p, ((ID3D12Resource*)resource.Handle)->GetGPUVirtualAddress());
    //                        return;
    //                    }

    //                    case RootParameterType.TypeDescriptorTable: {
    //                        ref readonly var table = ref param.DescriptorTable;

    //                        for (uint r = 0; r < table.NumDescriptorRanges; r++) {
    //                            ref readonly var range = ref table.PDescriptorRanges[r];

    //                            if (range.RegisterSpace != rinfo.Location.Space) continue;
    //                            if (range.BaseShaderRegister > rinfo.Location.Register || rinfo.Location.Register >= range.BaseShaderRegister + range.NumDescriptors) continue;
    //                            //if (range.RangeType != DescriptorRangeType.Cbv) continue;

    //                            switch (range.RangeType) {
    //                                case DescriptorRangeType.Cbv: {
    //                                    if (rinfo.Type != ResourceType.ConstantBuffer) continue;

    //                                    var handle = _descCommitter.GetGraphicsResourceDescriptor(p, rinfo.Location.Register - range.BaseShaderRegister);

    //                                    _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
    //                                        BufferLocation = d3d12resource->GetGPUVirtualAddress(),
    //                                        SizeInBytes = (uint)rdesc.Width,
    //                                    }, handle);
    //                                    break;
    //                                }

    //                                case DescriptorRangeType.Srv: {
    //                                    if (!rinfo.Type.IsReadonly()) continue;

    //                                    var handle = _descCommitter.GetGraphicsResourceDescriptor(p, rinfo.Location.Register - range.BaseShaderRegister);

    //                                    ShaderResourceViewDesc desc;
    //                                    switch (rinfo.Type) {
    //                                        case ResourceType.Buffer:

    //                                            break;
    //                                    }

    //                                    _context.Device->CreateShaderResourceView(d3d12resource, &desc, handle);

    //                                    break;
    //                                }
    //                            }



    //                            //if (offset >= d3d12buffer.Size) {
    //                            //    _context.Device->CreateConstantBufferView(null, handle);
    //                            //} else {
    //                            //    _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
    //                            //        BufferLocation = ((ID3D12Resource*)d3d12buffer.ResourceHandle.Handle)->GetGPUVirtualAddress()),
    //                            //        SizeInBytes = d3d12buffer.Size - offset,
    //                            //    }, handle);
    //                            //}
    //                        }
    //                        break;
    //                    }
    //                }
    //            }
    //        }

    //        switch (rinfo.Type) {
    //            case ResourceType.ConstantBuffer: {
    //                for (uint p = 0; p < numRootParams; p++) {
    //                    ref readonly var param = ref rootParams[p];

    //                    switch (param.ParameterType) {
    //                        case RootParameterType.TypeCbv: {
    //                            ref readonly var descriptor = ref param.Descriptor;

    //                            if (descriptor.RegisterSpace != rinfo.Location.Space) continue;
    //                            if (descriptor.ShaderRegister != rinfo.Location.Register) continue;

    //                            if (buffer.Handle == 0) {
    //                                if (!_unboundedGraphicsRootDescriptorIndices.Contains(p)) {
    //                                    _unboundedGraphicsRootDescriptorIndices.Add(p);
    //                                }
    //                            } else {
    //                                _unboundedGraphicsRootDescriptorIndices.Remove(p);
    //                                pCommandList.SetGraphicsRootConstantBufferView(p, ((ID3D12Resource*)buffer.Handle)->GetGPUVirtualAddress() + bufferOffset);
    //                            }
    //                            return;
    //                        }

    //                        case RootParameterType.TypeDescriptorTable: {
    //                            ref readonly var table = ref param.DescriptorTable;

    //                            for (uint r = 0; r < table.NumDescriptorRanges; r++) {
    //                                ref readonly var range = ref table.PDescriptorRanges[r];

    //                                if (range.RegisterSpace != rinfo.Location.Space) continue;
    //                                if (range.BaseShaderRegister > rinfo.Location.Register || rinfo.Location.Register >= range.BaseShaderRegister + range.NumDescriptors) continue;
    //                                if (range.RangeType != DescriptorRangeType.Cbv) continue;

    //                                var handle = _descCommitter.GetGraphicsResourceDescriptor(p, rinfo.Location.Register - range.BaseShaderRegister);

    //                                if (offset >= d3d12buffer.Size) {
    //                                    _context.Device->CreateConstantBufferView(null, handle);
    //                                } else {
    //                                    _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
    //                                        BufferLocation = ((ID3D12Resource*)d3d12buffer.ResourceHandle.Handle)->GetGPUVirtualAddress() + (offset & (D3D12.ConstantBufferDataPlacementAlignment - 1)),
    //                                        SizeInBytes = d3d12buffer.Size - offset,
    //                                    }, handle);
    //                                }
    //                            }

    //                            break;
    //                        }
    //                    }
    //                }

    //                break;
    //            }
    //        }

    //        return;
    //    }
    //}

    public override void SetGraphicsReadonlyBuffer(ReadOnlySpan<char> name, NativeBufferHandle buffer, uint structuredSize, GraphicsFormat typedBufferFormat) {
        if (_graphicalShader == null) return;

        EnsureNotClosed();

        var reflector = Unsafe.As<D3D12ShaderReflector>(_graphicalShader.Reflector);
        foreach (ref readonly var rinfo in reflector.ReadonlyResourceInfos.AsSpan()) {
            if (!name.SequenceEqual(rinfo.Name)) continue;
            if (!rinfo.Type.IsReadonlyBuffer()) continue;

            ref readonly var rsdesc = ref _graphicalShader.GetRootSignatureDesc()->Desc10;

            var rootParams = rsdesc.PParameters;
            var numRootParams = rsdesc.NumParameters;

            for (uint p = 0; p < numRootParams; p++) {
                ref readonly var param = ref rootParams[p];

                switch (param.ParameterType) {
                    case RootParameterType.TypeCbv: {
                        ref readonly var descriptor = ref param.Descriptor;

                        if (descriptor.RegisterSpace != rinfo.Location.Space) continue;
                        if (descriptor.ShaderRegister != rinfo.Location.Register) continue;

                        if (rinfo.Type != ResourceType.ConstantBuffer) continue;

                        if (buffer.Handle == 0) {
                            if (_unboundedGraphicsRootDescriptorIndices.Contains(p)) {
                                _unboundedGraphicsRootDescriptorIndices.Add(p);
                            }
                        } else {
                            _unboundedGraphicsRootDescriptorIndices.Remove(p);
                            pCommandList.SetGraphicsRootConstantBufferView(p, ((ID3D12Resource*)buffer.Handle)->GetGPUVirtualAddress());
                        }
                        break;
                    }

                    case RootParameterType.TypeSrv: {
                        ref readonly var descriptor = ref param.Descriptor;

                        if (descriptor.RegisterSpace != rinfo.Location.Space) continue;
                        if (descriptor.ShaderRegister != rinfo.Location.Register) continue;
                        if (rinfo.Type is not ResourceType.Buffer and not ResourceType.TextureBuffer and not ResourceType.StructuredBuffer and not ResourceType.ByteAddressBuffer) continue;

                        if (buffer.Handle == 0) {
                            if (_unboundedGraphicsRootDescriptorIndices.Contains(p)) {
                                _unboundedGraphicsRootDescriptorIndices.Add(p);
                            }
                        } else {
                            _unboundedGraphicsRootDescriptorIndices.Remove(p);
                            pCommandList.SetGraphicsRootShaderResourceView(p, ((ID3D12Resource*)buffer.Handle)->GetGPUVirtualAddress());
                        }
                        break;
                    }

                    case RootParameterType.TypeDescriptorTable:
                        ref readonly var table = ref param.DescriptorTable;

                        for (uint r = 0; r < table.NumDescriptorRanges; r++) {
                            ref readonly var range = ref table.PDescriptorRanges[r];

                            if (range.RegisterSpace != rinfo.Location.Space) continue;
                            if (range.BaseShaderRegister > rinfo.Location.Register || rinfo.Location.Register >= range.BaseShaderRegister + range.NumDescriptors) continue;

                            var handle = _descCommitter.GetGraphicsResourceDescriptor(p, rinfo.Location.Register - range.BaseShaderRegister);

                            switch (range.RangeType) {
                                case DescriptorRangeType.Cbv: {
                                    if (rinfo.Type != ResourceType.ConstantBuffer) continue;

                                    if (buffer.Handle == 0) {
                                        _context.Device->CreateConstantBufferView(null, handle);
                                    } else {
                                        ResourceDesc rdesc = ((ID3D12Resource*)buffer.Handle)->GetDesc();

                                        _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
                                            BufferLocation = ((ID3D12Resource*)buffer.Handle)->GetGPUVirtualAddress(),
                                            SizeInBytes = (uint)rdesc.Width,
                                        }, handle);
                                    }
                                    break;
                                }

                                case DescriptorRangeType.Srv: {
                                    if (rinfo.Type is not ResourceType.Buffer and not ResourceType.TextureBuffer and not ResourceType.StructuredBuffer and not ResourceType.ByteAddressBuffer) continue;

                                    ShaderResourceViewDesc vdesc = new() {
                                        ViewDimension = SrvDimension.Buffer,
                                        Shader4ComponentMapping = D3D12Helper.DefaultShader4ComponentMapping,
                                    };

                                    if (buffer.Handle == 0) {
                                        switch (rinfo.Type) {
                                            case ResourceType.Buffer:
                                                if (!D3D12Convert.TryConvert(typedBufferFormat, out var dxgiFormat) || !Unsafe.As<D3D12CapabilityChecker>(_context.CapabilityChecker).CheckFormatSupport(dxgiFormat, FormatSupport1.Buffer)) {
                                                    throw new ArgumentException($"Trying to bind Typed Buffer (Buffer<T>) with an unsupported format of '{typedBufferFormat}'.");
                                                }

                                                vdesc.Format = dxgiFormat;
                                                break;

                                            case ResourceType.TextureBuffer:
                                                vdesc.Format = Format.FormatR32Float;
                                                break;

                                            case ResourceType.StructuredBuffer:
                                                vdesc.Buffer.StructureByteStride = structuredSize;
                                                break;

                                            case ResourceType.ByteAddressBuffer:
                                                vdesc.Format = Format.FormatR32Uint;
                                                vdesc.Buffer.Flags = BufferSrvFlags.Raw;
                                                break;
                                        }

                                        _context.Device->CreateShaderResourceView(null, &vdesc, handle);
                                    } else {
                                        var d3d12resource = (ID3D12Resource*)buffer.Handle;
                                        var rdesc = d3d12resource->GetDesc();

                                        switch (rinfo.Type) {
                                            case ResourceType.Buffer:
                                                if (!D3D12Convert.TryConvert(typedBufferFormat, out var dxgiFormat) || !Unsafe.As<D3D12CapabilityChecker>(_context.CapabilityChecker).CheckFormatSupport(dxgiFormat, FormatSupport1.Buffer)) {
                                                    throw new ArgumentException($"Trying to bind Typed Buffer (Buffer<T>) with an unsupported format of '{typedBufferFormat}'.");
                                                }

                                                bool op = typedBufferFormat.TryGetStride(out var stride);
                                                Debug.Assert(op);

                                                vdesc.Format = dxgiFormat;
                                                vdesc.Buffer.NumElements = (uint)(rdesc.Width / stride);
                                                break;

                                            case ResourceType.TextureBuffer:
                                                vdesc.Format = Format.FormatR32Float;
                                                vdesc.Buffer.NumElements = (uint)(rdesc.Width / 4);
                                                break;

                                            case ResourceType.StructuredBuffer:
                                                vdesc.Buffer.StructureByteStride = structuredSize;
                                                vdesc.Buffer.NumElements = (uint)(rdesc.Width / structuredSize);
                                                break;

                                            case ResourceType.ByteAddressBuffer:
                                                vdesc.Format = Format.FormatR32Uint;
                                                vdesc.Buffer.Flags = BufferSrvFlags.Raw;
                                                vdesc.Buffer.NumElements = (uint)(rdesc.Width / 4);
                                                break;
                                        }

                                        _context.Device->CreateShaderResourceView((ID3D12Resource*)buffer.Handle, &vdesc, handle);
                                    }
                                    break;
                                }
                            }
                        }

                        break;
                }
            }

            break;
        }
    }

    public override void SetGraphicsReadonlyTexture(ReadOnlySpan<char> name, TextureViewHandle viewHandle) {
        if (_graphicalShader == null) return;

        EnsureNotClosed();

        var reflector = Unsafe.As<D3D12ShaderReflector>(_graphicalShader.Reflector);
        foreach (ref readonly var rinfo in reflector.ReadonlyResourceInfos.AsSpan()) {
            if (!name.SequenceEqual(rinfo.Name)) continue;

            ref readonly var rsdesc = ref _graphicalShader.GetRootSignatureDesc()->Desc10;

            var rootParams = rsdesc.PParameters;
            var numRootParams = rsdesc.NumParameters;

            if (D3D12Helper.TryFindTextureBindingLocation(rinfo.Location, rootParams, numRootParams, DescriptorRangeType.Srv, out var pindex, out var doffset)) {
                var handle = _descCommitter.GetGraphicsResourceDescriptor(pindex, doffset);

                _context.Device->CopyDescriptorsSimple(1, handle, new() { Ptr = (nuint)viewHandle.Handle }, DescriptorHeapType.CbvSrvUav);
                break;
            }
        }
    }

    //public override void SetComputeDynamicConstantBuffer(ReadOnlySpan<char> name, ReadOnlySpan<byte> data) {
    //    if (_computeShader == null || data.IsEmpty) return;

    //    var reflector = Unsafe.As<D3D12ShaderReflector>(_computeShader.Reflector);
    //    foreach (ref readonly var rinfo in reflector.ConstantBufferInfos.AsSpan()) {
    //        if (!name.SequenceEqual(rinfo.Name)) continue;

    //        ref readonly var rsdesc = ref _computeShader.GetRootSignatureDesc()->Desc10;

    //        var rootParams = rsdesc.PParameters;
    //        var numRootParams = rsdesc.NumParameters;

    //        for (uint i = 0; i < numRootParams; i++) {
    //            ref readonly var param = ref rootParams[i];

    //            switch (param.ParameterType) {
    //                case RootParameterType.Type32BitConstants:
    //                    ref readonly var consts = ref param.Constants;

    //                    if (consts.ShaderRegister != rinfo.Location.Register) continue;
    //                    if (consts.RegisterSpace != rinfo.Location.Space) continue;

    //                    pCommandList.SetComputeRoot32BitConstants(i, (uint)data.Length / sizeof(uint), data, 0);
    //                    return;

    //                case RootParameterType.TypeCbv: {
    //                    ref readonly var desc = ref param.Descriptor;

    //                    if (desc.ShaderRegister != rinfo.Location.Register) continue;
    //                    if (desc.RegisterSpace != rinfo.Location.Space) continue;

    //                    uint dataLength = uint.Min((uint)data.Length, 65_536); // 65536 bytes is the maximum size of constant buffer in D3D12.

    //                    _unboundedComputeRootDescriptorIndices.Remove(i);
    //                    var allocation = _uploadBuffer.Allocate(dataLength, D3D12.ConstantBufferDataPlacementAlignment);

    //                    Unsafe.CopyBlock(allocation.CpuAddress, Unsafe.AsPointer(ref MemoryMarshal.GetReference(data)), dataLength);

    //                    pCommandList.SetComputeRootConstantBufferView(i, allocation.VirtualAddress);
    //                    return;
    //                }

    //                case RootParameterType.TypeDescriptorTable: {
    //                    ref readonly var table = ref param.DescriptorTable;

    //                    for (uint r = 0; r < table.NumDescriptorRanges; r++) {
    //                        ref readonly var range = ref table.PDescriptorRanges[r];

    //                        if (range.RangeType != DescriptorRangeType.Cbv) continue;
    //                        if (range.RegisterSpace != rinfo.Location.Space) continue;
    //                        if (range.BaseShaderRegister > rinfo.Location.Register || rinfo.Location.Register >= range.BaseShaderRegister + range.NumDescriptors) continue;

    //                        uint allocateAmount = MathUtils.AlignUpwardPow2(uint.Max((uint)data.Length, 65_536), 256U);

    //                        var allocation = _uploadBuffer.Allocate(allocateAmount, D3D12.ConstantBufferDataPlacementAlignment);
    //                        Unsafe.CopyBlock(allocation.CpuAddress, Unsafe.AsPointer(ref MemoryMarshal.GetReference(data)), (uint)data.Length);

    //                        var handle = _descCommitter.GetComputeResourceDescriptor(i, rinfo.Location.Register - range.BaseShaderRegister);

    //                        _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
    //                            BufferLocation = allocation.VirtualAddress,
    //                            SizeInBytes = allocateAmount,
    //                        }, handle);

    //                        return;
    //                    }
    //                    break;
    //                }
    //            }
    //        }

    //        break;
    //    }
    //}

    //public override void SetComputeReadWriteBuffer(ReadOnlySpan<char> name, GpuBuffer buffer, uint offset) {
    //    //if (_computeShader == null) return;
    //    //if (!buffer.Flags.HasFlag(ResourceFlags.UnorderedAccess)) return;

    //    //var reflector = Unsafe.As<D3D12ShaderReflector>(_computeShader.Reflector);
    //    //foreach (ref readonly var rinfo in reflector.ReadWriteResourceInfos.AsSpan()) {
    //    //    if (!name.SequenceEqual(rinfo.Name)) continue;

    //    //    ref readonly var rsdesc = ref _computeShader.GetRootSignatureDesc()->Desc10;

    //    //    var rootParams = rsdesc.PParameters;
    //    //    var numRootParams = rsdesc.NumParameters;

    //    //    switch (rinfo.Type) {
    //    //        case ResourceType.RWStructuredBuffer:
    //    //            if (buffer is not D3D12StructuredBuffer d3d12sbuffer) return;

    //    //            for (uint p = 0; p < numRootParams; p++) {
    //    //                ref readonly var param = ref rootParams[p];

    //    //                switch (param.ParameterType) {
    //    //                    case RootParameterType.TypeUav:
    //    //                        ref readonly var descriptor = ref param.Descriptor;

    //    //                        if (descriptor.RegisterSpace != rinfo.Location.Space) continue;
    //    //                        if (descriptor.ShaderRegister != rinfo.Location.Register) continue;

    //    //                        _unboundedComputeRootDescriptorIndices.Remove(p);

    //    //                        pCommandList.SetComputeRootUnorderedAccessView(p, ((ID3D12Resource*)d3d12sbuffer.NativeResource)->GetGPUVirtualAddress() + offset * d3d12sbuffer.Stride);
    //    //                        return;

    //    //                    case RootParameterType.TypeDescriptorTable:
    //    //                        ref readonly var table = ref param.DescriptorTable;

    //    //                        for (uint r = 0; r < table.NumDescriptorRanges; r++) {
    //    //                            ref readonly var range = ref table.PDescriptorRanges[r];

    //    //                            if (range.RangeType != DescriptorRangeType.Srv) continue;
    //    //                            if (range.RegisterSpace != rinfo.Location.Space) continue;
    //    //                            if (range.BaseShaderRegister > rinfo.Location.Register || rinfo.Location.Register >= range.BaseShaderRegister + range.NumDescriptors) continue;

    //    //                            var handle = _descCommitter.GetComputeResourceDescriptor(p, rinfo.Location.Register - range.BaseShaderRegister);

    //    //                            UnorderedAccessViewDesc desc = new() {
    //    //                                ViewDimension = UavDimension.Buffer,
    //    //                                Buffer = new() {
    //    //                                    StructureByteStride = d3d12sbuffer.Stride,
    //    //                                },
    //    //                            };

    //    //                            if (offset >= d3d12sbuffer.NumElements) {
    //    //                                _context.Device->CreateUnorderedAccessView(null, null, &desc, handle);
    //    //                            } else {
    //    //                                desc.Buffer.FirstElement = offset;
    //    //                                desc.Buffer.NumElements = d3d12sbuffer.NumElements - offset;

    //    //                                _context.Device->CreateUnorderedAccessView((ID3D12Resource*)d3d12sbuffer.NativeResource, null, &desc, handle);
    //    //                            }

    //    //                            return;
    //    //                        }
    //    //                        break;
    //    //                }
    //    //            }
    //    //            break;
    //    //    }

    //    //    break;
    //    //}
    //}
}