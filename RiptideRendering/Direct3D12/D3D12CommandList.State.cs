namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12CommandList {
    public override void SetIndexBuffer(NativeResourceHandle resource, IndexFormat format, uint offset) {
        EnsureNotClosed();

        if (resource.Handle == 0) {
            pCommandList.IASetIndexBuffer((IndexBufferView*)null);
        } else {
            var d3d12resource = (ID3D12Resource*)resource.Handle;
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

    public override void SetGraphicsPipeline(PipelineState pipelineState, ResourceSignature resourceSignature, ReadOnlySpan<ConstantBufferBinding> constantBuffers, ReadOnlySpan<ReadonlyResourceBinding> readonlyResources, ReadOnlySpan<UnorderedAccessResourceBinding> unorderedAccesses) {
        if (pipelineState is not D3D12PipelineState d3d12pso) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "PipelineState", "Direct3D12's PipelineState"), nameof(pipelineState));
        if (resourceSignature is not D3D12ResourceSignature d3d12rs) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "ResourceSignature", "Direct3D12's ResourceSignature"), nameof(resourceSignature));
        
        SetPipelineState(d3d12pso);

        EnsureNotClosed();

        pCommandList.SetGraphicsRootSignature(d3d12rs.RootSignature);
        _descCommitter.InitializeGraphicDescriptors(d3d12rs.RootParameters, d3d12pso.Shader);

        foreach (ref readonly var cbbinding in constantBuffers) {
            if (!_descCommitter.TryGetGraphicsResourceDescriptor(cbbinding.TableIndex, cbbinding.ResourceOffset, out var descriptor)) continue;

            if (cbbinding.Resource.Handle == 0) {
                _context.Device->CreateConstantBufferView(null, descriptor);
            } else {
                ID3D12Resource* pResource = (ID3D12Resource*)cbbinding.Resource.Handle;
                ulong width = (uint)pResource->GetDesc().Width;
                uint offset = cbbinding.Offset;

                if (offset > width) {
                    _context.Device->CreateConstantBufferView(null, descriptor);
                } else {
                    _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
                        BufferLocation = pResource->GetGPUVirtualAddress() + offset,
                        SizeInBytes = (uint)pResource->GetDesc().Width - offset,
                    }, descriptor);
                }
            }
        }

        foreach (ref readonly var srvbinding in readonlyResources) {
            if (!_descCommitter.TryGetGraphicsResourceDescriptor(srvbinding.TableIndex, srvbinding.ResourceOffset, out var descriptor)) continue;

            _context.Device->CopyDescriptorsSimple(1, descriptor, new() { Ptr = (nuint)srvbinding.View.Handle }, DescriptorHeapType.CbvSrvUav);
        }

        foreach (ref readonly var uavbinding in unorderedAccesses) {
            if (!_descCommitter.TryGetGraphicsResourceDescriptor(uavbinding.TableIndex, uavbinding.ResourceOffset, out var descriptor)) continue;

            _context.Device->CopyDescriptorsSimple(1, descriptor, new() { Ptr = (nuint)uavbinding.View.Handle }, DescriptorHeapType.CbvSrvUav);
        }
    }

    private void SetPipelineState(D3D12PipelineState state) {
        EnsureNotClosed();

        switch (state.Type) {
            case PipelineStateType.Graphical:
                var d3d12gshader = Unsafe.As<D3D12GraphicalShader>(state.Shader);

                pCommandList.IASetPrimitiveTopology(d3d12gshader.HasTessellationStages ? D3DPrimitiveTopology.D3DPrimitiveTopology3ControlPointPatchlist : D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
                break;
        }

        pCommandList.SetPipelineState(state.PipelineState);

        _descCommitter.FinalizeResources();
    }

    //public override void SetGraphicsReadonlyBuffer(EncodedResourceLocation location, NativeBufferHandle buffer, uint structuredSize, GraphicsFormat typedBufferFormat) {
    //    var encoded = location.EncodedPosition;

    //    if ((encoded & (0b1UL << 63)) == 0) {
    //        pCommandList.SetGraphicsRootConstantBufferView((uint)encoded, ((ID3D12Resource*)buffer.Handle)->GetGPUVirtualAddress());
    //    } else {
    //        var descriptor = _descCommitter.GetGraphicsResourceDescriptor((uint)(encoded >> 32) & int.MaxValue, (uint)encoded);

    //        if (buffer.Handle == 0) {
    //            _context.Device->CreateConstantBufferView(null, descriptor);
    //        } else {
    //            _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
    //                BufferLocation = ((ID3D12Resource*)buffer.Handle)->GetGPUVirtualAddress(),
    //                SizeInBytes = (uint)((ID3D12Resource*)buffer.Handle)->GetDesc().Width,
    //            }, descriptor);
    //        }
    //    }
    //}

    //public override void SetGraphicsReadonlyTexture(EncodedResourceLocation location, NativeTextureView texture) {
    //    var encoded = location.EncodedPosition;

    //    var descriptor = _descCommitter.GetGraphicsResourceDescriptor((uint)(encoded >> 32) & int.MaxValue, (uint)encoded);
    //    _context.Device->CopyDescriptorsSimple(1, descriptor, new() { Ptr = (nuint)texture.Handle }, DescriptorHeapType.CbvSrvUav);
    //}
}