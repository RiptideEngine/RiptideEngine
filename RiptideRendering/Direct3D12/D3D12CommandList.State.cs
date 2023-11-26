namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12CommandList {
    public override void SetIndexBuffer(GpuBuffer? buffer, IndexFormat format, uint offset) {
        EnsureNotClosed();

        if (buffer == null) {
            pCommandList.IASetIndexBuffer((IndexBufferView*)null);
        } else {
            Debug.Assert(buffer is D3D12GpuBuffer, "buffer is D3D12GpuBuffer");
            FlushResourceBarriers();
            
            var d3d12resource = (ID3D12Resource*)buffer.NativeResourceHandle;
            var rdesc = d3d12resource->GetDesc();

            if (offset >= rdesc.Width) {
                pCommandList.IASetIndexBuffer((IndexBufferView*)null);
            } else {
                if (!format.IsDefined()) throw new ArgumentException("Undefined format enumeration.");

                IndexBufferView view = new() {
                    BufferLocation = ((ID3D12Resource*)buffer.NativeResourceHandle)->GetGPUVirtualAddress() + offset,
                    SizeInBytes = (uint)(rdesc.Width - offset),
                    Format = (Format)((uint)Format.FormatR16Uint - 15 * (uint)format),
                };

                pCommandList.IASetIndexBuffer(&view);
            }
        }
    }

    public override void SetPipelineState(PipelineState pipelineState) {
        if (pipelineState is not D3D12PipelineState d3d12pso) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "PipelineState", "Direct3D12's PipelineState"), nameof(pipelineState));

        EnsureNotClosed();

        pCommandList.SetPipelineState(d3d12pso.PipelineState);
    }

    public override void SetPrimitiveTopology(RenderingPrimitiveTopology topology) {
        switch (topology) {
            case RenderingPrimitiveTopology.Undefined: break;
            case RenderingPrimitiveTopology.PointList: pCommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyPointlist); break;
            case RenderingPrimitiveTopology.LineList: pCommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyLinelist); break;
            case RenderingPrimitiveTopology.LineStrip: pCommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3D10PrimitiveTopologyLinestrip); break;
            case RenderingPrimitiveTopology.TriangleList: pCommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist); break;
            case RenderingPrimitiveTopology.TriangleStrip: pCommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglestrip); break;
            default:
                if (topology is >= RenderingPrimitiveTopology.ControlPointPatchList1 and <= RenderingPrimitiveTopology.ControlPointPatchList32) {
                    pCommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopology1ControlPointPatchlist + (topology - RenderingPrimitiveTopology.ControlPointPatchList1));
                }
                break;
        }
    }

    public override void SetGraphicsResourceSignature(ResourceSignature signature) {
        if (signature is not D3D12ResourceSignature d3d12rs) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "ResourceSignature", "Direct3D12's ResourceSignature"), nameof(signature));

        EnsureNotClosed();

        pCommandList.SetGraphicsRootSignature(d3d12rs.RootSignature);
        _descCommitter.InitializeGraphicDescriptors(d3d12rs);
    }

    public override void SetGraphicsConstants(uint parameterIndex, ReadOnlySpan<uint> constants, uint offset) {
        EnsureNotClosed();

        pCommandList.SetGraphicsRoot32BitConstants(parameterIndex, (uint)constants.Length, Unsafe.AsPointer(ref MemoryMarshal.GetReference(constants)), offset);
    }

    public override void SetGraphicsConstantBuffer(uint tableIndex, uint tableOffset, GpuBuffer buffer, uint offset) {
        Debug.Assert(buffer is D3D12GpuBuffer, "buffer is D3D12GpuBuffer");
        
        EnsureNotClosed();
        
        if (!_descCommitter.TryGetGraphicsResourceDescriptor(tableIndex, tableOffset, out var descriptor)) return;
        
        FlushResourceBarriers();

        ID3D12Resource* pResource = (ID3D12Resource*)buffer.NativeResourceHandle;

        if (pResource == null) {
            _context.Device->CreateConstantBufferView(null, descriptor);
        } else {
            ulong width = (uint)pResource->GetDesc().Width;

            if (offset > width) {
                _context.Device->CreateConstantBufferView(null, descriptor);
            } else {
                _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc {
                    BufferLocation = pResource->GetGPUVirtualAddress() + offset,
                    SizeInBytes = (uint)pResource->GetDesc().Width - offset + 255U & ~255U,
                }, descriptor);
            }
        }
    }

    public override void SetGraphicsShaderResourceView(uint tableIndex, uint tableOffset, ShaderResourceView? view) {
        EnsureNotClosed();
        
        if (!_descCommitter.TryGetGraphicsResourceDescriptor(tableIndex, tableOffset, out var descriptor)) return;

        FlushResourceBarriers();
        
        if (view == null) {
            _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                ViewDimension = SrvDimension.Texture2D,
                Format = Format.FormatR8G8B8A8Unorm,
                Shader4ComponentMapping = D3D12Helper.DefaultShader4ComponentMapping,
                Texture2D = new() {
                    MipLevels = 1,
                    MostDetailedMip = 0,
                    ResourceMinLODClamp = 0,
                    PlaneSlice = 0,
                },
            }, descriptor);
        } else {
            Debug.Assert(view is D3D12ShaderResourceView, "view is D3D12ShaderResourceView");
            
            _context.Device->CopyDescriptorsSimple(1, descriptor, Unsafe.As<D3D12ShaderResourceView>(view).Handle, DescriptorHeapType.CbvSrvUav);
        }
    }

    //public override void SetGraphicsPipeline(PipelineState state, ReadOnlySpan<ConstantBufferBinding> constantBuffers, ReadOnlySpan<ReadonlyResourceBinding> readonlyResources, ReadOnlySpan<UnorderedAccessResourceBinding> unorderedAccesses) {
    //    if (state is not D3D12PipelineState d3d12pso) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "PipelineState", "Direct3D12's PipelineState"), nameof(state));

    //    SetPipelineState(d3d12pso);

    //    EnsureNotClosed();

    //    foreach (ref readonly var cbbinding in constantBuffers) {
    //        if (!_descCommitter.TryGetGraphicsResourceDescriptor(cbbinding.TableIndex, cbbinding.ResourceOffset, out var descriptor)) continue;

    //        if (cbbinding.Resource.Handle == 0) {
    //            _context.Device->CreateConstantBufferView(null, descriptor);
    //        } else {
    //            ID3D12Resource* pResource = (ID3D12Resource*)cbbinding.Resource.Handle;
    //            ulong width = (uint)pResource->GetDesc().Width;
    //            uint offset = cbbinding.Offset;

    //            if (offset > width) {
    //                _context.Device->CreateConstantBufferView(null, descriptor);
    //            } else {
    //                _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc() {
    //                    BufferLocation = pResource->GetGPUVirtualAddress() + offset,
    //                    SizeInBytes = (uint)pResource->GetDesc().Width - offset,
    //                }, descriptor);
    //            }
    //        }
    //    }

    //    foreach (ref readonly var srvbinding in readonlyResources) {
    //        if (!_descCommitter.TryGetGraphicsResourceDescriptor(srvbinding.TableIndex, srvbinding.ResourceOffset, out var descriptor)) continue;

    //        _context.Device->CopyDescriptorsSimple(1, descriptor, new() { Ptr = (nuint)srvbinding.View.Handle }, DescriptorHeapType.CbvSrvUav);
    //    }

    //    foreach (ref readonly var uavbinding in unorderedAccesses) {
    //        if (!_descCommitter.TryGetGraphicsResourceDescriptor(uavbinding.TableIndex, uavbinding.ResourceOffset, out var descriptor)) continue;

    //        _context.Device->CopyDescriptorsSimple(1, descriptor, new() { Ptr = (nuint)uavbinding.View.Handle }, DescriptorHeapType.CbvSrvUav);
    //    }
    //}

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