using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Maths;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12GraphicsCommandList : CommandList {
    public const string UnnamedCommandList = $"<Unnamed {nameof(D3D12GraphicsCommandList)}>.{nameof(pCommandList)}";

    private readonly D3D12RenderingContext _context;

    private ComPtr<ID3D12GraphicsCommandList> pCommandList;
    private ID3D12CommandAllocator* pAllocator;

    private readonly List<ResourceBarrier> _barriers;

    private readonly UploadBufferProvider _uploadProvider;
    private readonly GraphicsDescriptorCommitter _descCommitter;

    public ID3D12GraphicsCommandList* CommandList => pCommandList;

    public override string? Name {
        get => base.Name;
        set {
            if (Interlocked.Read(ref _refcount) == 0) return;
            
            base.Name = value;

            if (pCommandList.Handle != null) {
                Helper.SetName(pCommandList.Handle, $"{value}.{nameof(pCommandList)}");
            }
        }
    }

    public D3D12GraphicsCommandList(D3D12RenderingContext context) {
        _context = context;

        var queue = _context.Queues.GraphicsQueue;
        pAllocator = queue.RequestAllocator();
        
        int hr = context.Device->CreateCommandList(0, D3D12CommandListType.Direct, pAllocator, null, SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList>(), (void**)pCommandList.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);
        
        Helper.SetName(pCommandList.Handle, UnnamedCommandList);

        _descCommitter = new(context);
        
        _barriers = [];
        IsClosed = false;
        
        _uploadProvider = new();
        _uploadProvider.RequestResource(context.UploadBufferPool, D3D12.DefaultResourcePlacementAlignment, D3D12CommandListType.Direct, queue.LastCompletedFenceValue);
    }

    public override void TranslateResourceState(GpuResource resource, ResourceTranslateStates destinationState) {
        TranslateResourceState(resource, 0xFFFFFFFF, destinationState);
    }

    public override void TranslateResourceState(GpuResource resource, uint subresource, ResourceTranslateStates destinationState) {
        EnsureNotClosed();
        
        CommandListOperations.AddResourceTransitionBarrier(resource, subresource, destinationState, _barriers);
    }

    public override void CopyResource(GpuResource dest, GpuResource source) {
        ArgumentNullException.ThrowIfNull(dest, nameof(dest));
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        if (dest == source) throw new ArgumentException("Source and destination resources must be different resource.");
        
        Debug.Assert(dest is D3D12GpuBuffer or D3D12GpuTexture, "dest is D3D12GpuBuffer or D3D12GpuTexture");
        Debug.Assert(source is D3D12GpuBuffer or D3D12GpuTexture, "source is D3D12GpuBuffer or D3D12GpuTexture");
        
        EnsureNotClosed();

        pCommandList.CopyResource((ID3D12Resource*)dest.NativeResourceHandle, (ID3D12Resource*)source.NativeResourceHandle);
    }

    public override void CopyBufferRegion(GpuBuffer dest, ulong destOffset, GpuBuffer source, ulong sourceOffset, ulong numBytes) {
        ArgumentNullException.ThrowIfNull(dest, nameof(dest));
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        
        Debug.Assert(dest is D3D12GpuBuffer, "dest is D3D12GpuBuffer");
        Debug.Assert(source is D3D12GpuBuffer, "source is D3D12GpuBuffer");
        
        EnsureNotClosed();
        
        pCommandList.CopyBufferRegion((ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(dest).NativeResourceHandle, destOffset, (ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(source).NativeResourceHandle, sourceOffset, numBytes);
    }
    
    public override void UpdateBuffer(GpuBuffer buffer, ReadOnlySpan<byte> data) {
        EnsureNotClosed();

        Debug.Assert(buffer is D3D12GpuBuffer, "dest is D3D12GpuBuffer");
        
        ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(buffer).NativeResourceHandle;
        ResourceDesc rdesc = pResource->GetDesc();

        uint writeAmount = uint.Min((uint)rdesc.Width, (uint)data.Length);
        var uploadRegion = AllocateUploadRegion(writeAmount, 1);
        
        data.CopyTo(new(uploadRegion.Pointer, (int)writeAmount));
        
        FlushResourceBarriers();
        pCommandList.CopyBufferRegion(pResource, 0, uploadRegion.Resource, uploadRegion.Offset, writeAmount);
    }

    public override void UpdateBuffer(GpuBuffer buffer, uint offset, ReadOnlySpan<byte> data) {
        EnsureNotClosed();

        Debug.Assert(buffer is D3D12GpuBuffer, "dest is D3D12GpuBuffer");
        
        ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(buffer).NativeResourceHandle;
        ResourceDesc rdesc = pResource->GetDesc();

        if (offset >= rdesc.Width) return;

        uint writeAmount = uint.Min((uint)data.Length, (uint)(rdesc.Width - offset));
        var uploadRegion = AllocateUploadRegion(writeAmount, 1);
        
        data.CopyTo(new(uploadRegion.Pointer, (int)writeAmount));
        
        FlushResourceBarriers();
        pCommandList.CopyBufferRegion(pResource, offset, uploadRegion.Resource, uploadRegion.Offset, writeAmount);
    }

    public override void UpdateBuffer<T>(GpuBuffer buffer, BufferWriter<T> writer, T state) {
        EnsureNotClosed();
        
        Debug.Assert(buffer is D3D12GpuBuffer, "dest is D3D12GpuBuffer");
        
        ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(buffer).NativeResourceHandle;
        ResourceDesc rdesc = pResource->GetDesc();

        var uploadRegion = AllocateUploadRegion(rdesc.Width, 1);
        writer(new(uploadRegion.Pointer, (int)rdesc.Width), state);
        
        FlushResourceBarriers();
        pCommandList.CopyBufferRegion(pResource, 0, uploadRegion.Resource, uploadRegion.Offset, rdesc.Width);
    }

    public override void UpdateBuffer<T>(GpuBuffer buffer, uint offset, uint length, BufferWriter<T> writer, T arg) {
        EnsureNotClosed();
        
        Debug.Assert(buffer is D3D12GpuBuffer, "dest is D3D12GpuBuffer");
        
        ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(buffer).NativeResourceHandle;
        ResourceDesc rdesc = pResource->GetDesc();

        if (offset >= rdesc.Width) return;

        uint writeAmount = uint.Min(length, (uint)(rdesc.Width - offset));
        var uploadRegion = AllocateUploadRegion(writeAmount, 1);

        writer(new(uploadRegion.Pointer, (int)writeAmount), arg);
        
        FlushResourceBarriers();
        pCommandList.CopyBufferRegion(pResource, offset, uploadRegion.Resource, uploadRegion.Offset, writeAmount);
    }
    
    public override void UpdateTexture(GpuTexture texture, uint subresource, ReadOnlySpan<byte> data) {
        EnsureNotClosed();
        
        Debug.Assert(texture is D3D12GpuTexture, "dest is D3D12GpuTexture");
        
        ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuTexture>(texture).NativeResourceHandle;
        ResourceDesc rdesc = pResource->GetDesc();

        PlacedSubresourceFootprint footprint;
        uint numRows;
        ulong rowSize;
        ulong totalBytes;
        
        _context.Device->GetCopyableFootprints(&rdesc, subresource, 1, 0, &footprint, &numRows, &rowSize, &totalBytes);

        if ((ulong)data.Length < numRows * rowSize) throw new ArgumentException($"Not enough data to update texture's subresource {subresource} of texture, {numRows * rowSize} bytes expected, but only {data.Length} bytes provided.");

        var uploadRegion = AllocateUploadRegion(totalBytes, D3D12.TextureDataPlacementAlignment);
        footprint.Offset = uploadRegion.Offset;
        
        var pitch = footprint.Footprint.RowPitch;

        fixed (byte* ptr = data) {
            byte* pSrc = ptr;
            byte* pDest = uploadRegion.Pointer;
            
            for (uint r = 0; r < numRows; r++) {
                Unsafe.CopyBlock(pDest, pSrc, (uint)rowSize);

                pSrc += rowSize;
                pDest += pitch;
            }
        }
        
        FlushResourceBarriers();
        pCommandList.CopyTextureRegion(new TextureCopyLocation {
            Type = TextureCopyType.SubresourceIndex,
            PResource = pResource,
            SubresourceIndex = subresource,
        }, 0, 0, 0, new TextureCopyLocation {
            Type = TextureCopyType.PlacedFootprint,
            PResource = uploadRegion.Resource,
            PlacedFootprint = footprint,
        }, null);
    }
    
    public override void UpdateTexture<T>(GpuTexture texture, uint subresource, TextureWriter<T> writer, T arg) {
        EnsureNotClosed();
        
        Debug.Assert(texture is D3D12GpuTexture, "dest is D3D12GpuTexture");
        
        ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuTexture>(texture).NativeResourceHandle;
        ResourceDesc rdesc = pResource->GetDesc();

        PlacedSubresourceFootprint footprint;
        uint numRows;
        ulong rowSize;
        ulong totalBytes;
        
        _context.Device->GetCopyableFootprints(&rdesc, subresource, 1, 0, &footprint, &numRows, &rowSize, &totalBytes);

        var uploadRegion = AllocateUploadRegion(totalBytes, D3D12.TextureDataPlacementAlignment);
        footprint.Offset = uploadRegion.Offset;
        
        var pitch = footprint.Footprint.RowPitch;

        byte* pDest = uploadRegion.Pointer;
        
        for (uint r = 0; r < numRows; r++) {
            writer(new(pDest, (int)rowSize), r, arg);
            pDest += pitch;
        }
        
        FlushResourceBarriers();
        pCommandList.CopyTextureRegion(new TextureCopyLocation {
            Type = TextureCopyType.SubresourceIndex,
            PResource = pResource,
            SubresourceIndex = subresource,
        }, 0, 0, 0, new TextureCopyLocation {
            Type = TextureCopyType.PlacedFootprint,
            PResource = uploadRegion.Resource,
            PlacedFootprint = footprint,
        }, null);
    }

    public override void ClearRenderTarget(RenderTargetView view, Color color) => ClearRenderTarget(view, color, []);
    public override void ClearRenderTarget(RenderTargetView view, Color color, ReadOnlySpan<Bound2DInt> areas) {
        EnsureNotClosed();
        
        Debug.Assert(view is D3D12RenderTargetView, "view is D3D12RenderTargetView");
        
        FlushResourceBarriers();

        fixed (Bound2DInt* pAreas = areas) {
            pCommandList.ClearRenderTargetView(Unsafe.As<D3D12RenderTargetView>(view).Handle, (float*)&color, (uint)areas.Length, (Box2D<int>*)pAreas);
        }
    }

    public override void ClearDepthStencil(DepthStencilView view, DepthClearFlags clearFlags, float depth, byte stencil) => ClearDepthStencil(view, clearFlags, depth, stencil, []);
    public override void ClearDepthStencil(DepthStencilView view, DepthClearFlags clearFlags, float depth, byte stencil, ReadOnlySpan<Bound2DInt> areas) {
        EnsureNotClosed();
        
        Debug.Assert(view is D3D12DepthStencilView, "view is D3D12DepthStencilView");
        
        FlushResourceBarriers();

        fixed (Bound2DInt* pAreas = areas) {
            pCommandList.ClearDepthStencilView(Unsafe.As<D3D12DepthStencilView>(view).Handle, (ClearFlags)clearFlags, depth, stencil, (uint)areas.Length, (Box2D<int>*)pAreas);
        }
    }

    public override void SetStencilRef(byte stencil) {
        EnsureNotClosed();
        
        pCommandList.OMSetStencilRef(stencil);
    }

    public override void SetRenderTarget(RenderTargetView view, DepthStencilView? depthView) {
        EnsureNotClosed();
        
        Debug.Assert(view is D3D12RenderTargetView, "view is D3D12RenderTargetView");

        CpuDescriptorHandle rtvHandle = Unsafe.As<D3D12RenderTargetView>(view).Handle;
        
        if (depthView == null) {
            pCommandList.OMSetRenderTargets(1, &rtvHandle, false, (CpuDescriptorHandle*)null);
        } else {
            Debug.Assert(depthView is D3D12DepthStencilView, "depthView is D3D12DepthStencilView");

            CpuDescriptorHandle dsvHandle = Unsafe.As<D3D12DepthStencilView>(depthView).Handle;
            pCommandList.OMSetRenderTargets(1, &rtvHandle, false, &dsvHandle);
        }
    }

    public override void SetRenderTargets(ReadOnlySpan<RenderTargetView> view, DepthStencilView? depthView) {
        EnsureNotClosed();

        var count = int.Min(8, view.Length);
        Span<CpuDescriptorHandle> handles = stackalloc CpuDescriptorHandle[count];

        for (int i = 0; i < count; i++) {
            Debug.Assert(view[i] is D3D12RenderTargetView, "view[i] is D3D12RenderTargetView");

            handles[i] = Unsafe.As<D3D12RenderTargetView>(view[i]).Handle;
        }

        fixed (CpuDescriptorHandle* pHandles = handles) {
            if (depthView == null) {
                pCommandList.OMSetRenderTargets((uint)count, pHandles, false, (CpuDescriptorHandle*)null);
            } else {
                Debug.Assert(depthView is D3D12DepthStencilView, "depthView is D3D12DepthStencilView");
                
                CpuDescriptorHandle dsvHandle = Unsafe.As<D3D12DepthStencilView>(depthView).Handle;
                pCommandList.OMSetRenderTargets((uint)count, pHandles, false, &dsvHandle);
            }
        }
    }

    public override void SetViewport(Viewport viewport) {
        EnsureNotClosed();
        
        pCommandList.RSSetViewports(1, (Silk.NET.Direct3D12.Viewport*)&viewport);
    }

    public override void SetScissorRect(Bound2DInt scissor) {
        EnsureNotClosed();

        pCommandList.RSSetScissorRects(1, (Box2D<int>*)&scissor);
    }

    public override void SetPrimitiveTopology(RenderingPrimitiveTopology topology) {
        EnsureNotClosed();

        pCommandList.IASetPrimitiveTopology(topology switch {
            RenderingPrimitiveTopology.Undefined => D3DPrimitiveTopology.D3DPrimitiveTopologyUndefined,
            RenderingPrimitiveTopology.PointList => D3DPrimitiveTopology.D3DPrimitiveTopologyPointlist,
            RenderingPrimitiveTopology.LineList => D3DPrimitiveTopology.D3DPrimitiveTopologyLinelist,
            RenderingPrimitiveTopology.LineStrip => D3DPrimitiveTopology.D3DPrimitiveTopologyLinestrip,
            RenderingPrimitiveTopology.TriangleList => D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist,
            RenderingPrimitiveTopology.TriangleStrip => D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglestrip,
            
            RenderingPrimitiveTopology.ControlPointPatchList1 => D3DPrimitiveTopology.D3DPrimitiveTopology1ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList2 => D3DPrimitiveTopology.D3DPrimitiveTopology2ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList3 => D3DPrimitiveTopology.D3DPrimitiveTopology3ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList4 => D3DPrimitiveTopology.D3DPrimitiveTopology4ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList5 => D3DPrimitiveTopology.D3DPrimitiveTopology5ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList6 => D3DPrimitiveTopology.D3DPrimitiveTopology6ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList7 => D3DPrimitiveTopology.D3DPrimitiveTopology7ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList8 => D3DPrimitiveTopology.D3DPrimitiveTopology8ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList9 => D3DPrimitiveTopology.D3DPrimitiveTopology9ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList10 => D3DPrimitiveTopology.D3DPrimitiveTopology10ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList11 => D3DPrimitiveTopology.D3DPrimitiveTopology11ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList12 => D3DPrimitiveTopology.D3DPrimitiveTopology12ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList13 => D3DPrimitiveTopology.D3DPrimitiveTopology13ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList14 => D3DPrimitiveTopology.D3DPrimitiveTopology14ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList15 => D3DPrimitiveTopology.D3DPrimitiveTopology15ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList16 => D3DPrimitiveTopology.D3DPrimitiveTopology16ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList17 => D3DPrimitiveTopology.D3DPrimitiveTopology17ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList18 => D3DPrimitiveTopology.D3DPrimitiveTopology18ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList19 => D3DPrimitiveTopology.D3DPrimitiveTopology19ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList20 => D3DPrimitiveTopology.D3DPrimitiveTopology20ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList21 => D3DPrimitiveTopology.D3DPrimitiveTopology21ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList22 => D3DPrimitiveTopology.D3DPrimitiveTopology22ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList23 => D3DPrimitiveTopology.D3DPrimitiveTopology23ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList24 => D3DPrimitiveTopology.D3DPrimitiveTopology24ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList25 => D3DPrimitiveTopology.D3DPrimitiveTopology25ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList26 => D3DPrimitiveTopology.D3DPrimitiveTopology26ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList27 => D3DPrimitiveTopology.D3DPrimitiveTopology27ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList28 => D3DPrimitiveTopology.D3DPrimitiveTopology28ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList29 => D3DPrimitiveTopology.D3DPrimitiveTopology29ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList30 => D3DPrimitiveTopology.D3DPrimitiveTopology30ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList31 => D3DPrimitiveTopology.D3DPrimitiveTopology31ControlPointPatchlist,
            RenderingPrimitiveTopology.ControlPointPatchList32 => D3DPrimitiveTopology.D3DPrimitiveTopology32ControlPointPatchlist,
            
            _ => D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist,
        });
    }

    public override void SetIndexBuffer(GpuBuffer? buffer, IndexFormat format, uint offset) {
        EnsureNotClosed();
        
        if (buffer == null) {
            pCommandList.IASetIndexBuffer((IndexBufferView*)null);
        } else {
            Debug.Assert(buffer is D3D12GpuBuffer, "buffer is D3D12GpuBuffer");

            ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(buffer).NativeResourceHandle;
            ResourceDesc rdesc = pResource->GetDesc();

            if (offset < rdesc.Width) {
                pCommandList.IASetIndexBuffer(new IndexBufferView {
                    BufferLocation = pResource->GetGPUVirtualAddress() + offset,
                    Format = format switch {
                        IndexFormat.UInt32 => Format.FormatR32Uint,
                        _ => Format.FormatR16Uint,
                    },
                    SizeInBytes = (uint)(rdesc.Width - offset),
                });
            }
        }
    }

    public override void SetPipelineState(PipelineState pipelineState) {
        EnsureNotClosed();
        
        Debug.Assert(pipelineState is D3D12PipelineState, "pipelineState is D3D12PipelineState");

        pCommandList.SetPipelineState(Unsafe.As<D3D12PipelineState>(pipelineState).PipelineState);
    }
    
    public override void SetGraphicsResourceSignature(ResourceSignature signature) {
        EnsureNotClosed();
        
        Debug.Assert(signature is D3D12ResourceSignature, "signature is D3D12ResourceSignature");

        var d3d12sig = Unsafe.As<D3D12ResourceSignature>(signature);

        pCommandList.SetGraphicsRootSignature(d3d12sig.RootSignature);
        _descCommitter.SetGraphicsResourceSignature(d3d12sig);
    }

    public override void SetGraphicsConstants(uint parameterIndex, ReadOnlySpan<ConstantParameterValue> constants, uint offset) {
        EnsureNotClosed();

        fixed (ConstantParameterValue* pConstants = constants) {
            pCommandList.SetGraphicsRoot32BitConstants(parameterIndex, (uint)constants.Length, pConstants, offset);
        }
    }

    public override void SetGraphicsConstantBufferView(uint parameterIndex, uint descriptorIndex, GpuBuffer? buffer, uint offset, uint size) {
        EnsureNotClosed();
        
        if (!_descCommitter.TryGetGraphicsResourceDescriptor(parameterIndex, descriptorIndex, out var handle)) return;
        FlushResourceBarriers();
        
        if (buffer == null || size == 0) {
            _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc {
                BufferLocation = 0,
                SizeInBytes = 0,
            }, handle);
        } else {
            Debug.Assert(buffer is D3D12GpuBuffer, "buffer is D3D12GpuBuffer");

            _context.Device->CreateConstantBufferView(new ConstantBufferViewDesc {
                BufferLocation = ((ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(buffer).NativeResourceHandle)->GetGPUVirtualAddress() + offset,
                SizeInBytes = size,
            }, handle);
        }
    }

    public override void SetGraphicsShaderResourceView(uint parameterIndex, uint descriptorIndex, ShaderResourceView view) {
        EnsureNotClosed();

        Debug.Assert(view is D3D12ShaderResourceView, "view is D3D12ShaderResourceView");
        
        if (!_descCommitter.TryGetGraphicsResourceDescriptor(parameterIndex, descriptorIndex, out var handle)) return;
        
        FlushResourceBarriers();
        _context.Device->CopyDescriptorsSimple(1, handle, Unsafe.As<D3D12ShaderResourceView>(view).Handle, DescriptorHeapType.CbvSrvUav);
    }

    public override void NullifyGraphicsShaderResourceView(uint parameterIndex, uint descriptorIndex, ShaderResourceViewDimension dimension) {
        EnsureNotClosed();
        
        if (!_descCommitter.TryGetGraphicsResourceDescriptor(parameterIndex, descriptorIndex, out var handle)) return;
        
        FlushResourceBarriers();
        switch (dimension) {
            case ShaderResourceViewDimension.Buffer:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Buffer,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32Float,
                    Buffer = new() {
                        FirstElement = 0,
                        NumElements = 0,
                    }
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture1D:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture1D,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture1DArray:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture1Darray,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture2D:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture2D,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture2DArray:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture2Darray,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture3D:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture3D,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.TextureCube:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texturecube,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.TextureCubeArray:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texturecubearray,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
        }
    }

    public override void Draw(uint vertexCount, uint instanceCount, uint startVertex, uint startInstance) {
        EnsureNotClosed();
        
        FlushResourceBarriers();
        _descCommitter.CommitGraphics(pCommandList);
        
        pCommandList.DrawInstanced(vertexCount, instanceCount, startVertex, startInstance);
    }

    public override void DrawIndexed(uint indexCount, uint instanceCount, uint startIndex, uint startInstance) {
        EnsureNotClosed();
        
        FlushResourceBarriers();
        _descCommitter.CommitGraphics(pCommandList);
        
        pCommandList.DrawIndexedInstanced(indexCount, instanceCount, startIndex, 0, startInstance);
    }

    public override void SetComputeResourceSignature(ResourceSignature signature) {
        EnsureNotClosed();
        
        Debug.Assert(signature is D3D12ResourceSignature, "signature is D3D12ResourceSignature");

        var d3d12sig = Unsafe.As<D3D12ResourceSignature>(signature);

        pCommandList.SetComputeRootSignature(d3d12sig.RootSignature);
        _descCommitter.SetComputeResourceSignature(d3d12sig);
    }

    public override void SetComputeShaderResourceView(uint parameterIndex, uint descriptorIndex, ShaderResourceView view) {
        EnsureNotClosed();

        Debug.Assert(view is D3D12ShaderResourceView, "view is D3D12ShaderResourceView");
        
        if (!_descCommitter.TryGetComputeResourceDescriptor(parameterIndex, descriptorIndex, out var handle)) return;
        
        FlushResourceBarriers();
        _context.Device->CopyDescriptorsSimple(1, handle, Unsafe.As<D3D12ShaderResourceView>(view).Handle, DescriptorHeapType.CbvSrvUav);
    }
    
    public override void NullifyComputeShaderResourceView(uint parameterIndex, uint descriptorIndex, ShaderResourceViewDimension dimension) {
        EnsureNotClosed();
        
        if (!_descCommitter.TryGetComputeResourceDescriptor(parameterIndex, descriptorIndex, out var handle)) return;
        
        FlushResourceBarriers();
        switch (dimension) {
            case ShaderResourceViewDimension.Buffer:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Buffer,
                    Format = Format.FormatR32Float,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Buffer = new() {
                        FirstElement = 0,
                        NumElements = 0,
                    }
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture1D:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture1D,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture1DArray:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture1Darray,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture2D:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture2D,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture2DArray:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture2Darray,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.Texture3D:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texture3D,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.TextureCube:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texturecube,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
            
            case ShaderResourceViewDimension.TextureCubeArray:
                _context.Device->CreateShaderResourceView(null, new ShaderResourceViewDesc {
                    ViewDimension = SrvDimension.Texturecubearray,
                    Shader4ComponentMapping = Helper.DefaultShader4ComponentMapping,
                    Format = Format.FormatR32G32B32A32Float,
                }, handle);
                break;
        }
    }

    public override void SetComputeUnorderedAccessView(uint parameterIndex, uint descriptorIndex, UnorderedAccessView view) {
        EnsureNotClosed();

        Debug.Assert(view is D3D12UnorderedAccessView, "view is D3D12UnorderedAccessView");
        
        if (!_descCommitter.TryGetComputeResourceDescriptor(parameterIndex, descriptorIndex, out var handle)) return;
        
        FlushResourceBarriers();
        _context.Device->CopyDescriptorsSimple(1, handle, Unsafe.As<D3D12UnorderedAccessView>(view).Handle, DescriptorHeapType.CbvSrvUav);
    }

    public override void NullifyComputeUnorderedAccessView(uint parameterIndex, uint descriptorIndex, UnorderedAccessViewDimension dimension) {
        EnsureNotClosed();
        
        if (!_descCommitter.TryGetComputeResourceDescriptor(parameterIndex, descriptorIndex, out var handle)) return;

        FlushResourceBarriers();
        switch (dimension) {
            case UnorderedAccessViewDimension.Buffer:
                _context.Device->CreateUnorderedAccessView(null, null, new UnorderedAccessViewDesc {
                    ViewDimension = UavDimension.Buffer,
                    Format = Format.FormatR32Float,
                }, handle);
                break;
            
            case UnorderedAccessViewDimension.Texture1D:
                _context.Device->CreateUnorderedAccessView(null, null, new UnorderedAccessViewDesc {
                    ViewDimension = UavDimension.Texture1D,
                    Format = Format.FormatR32G32B32Float,
                }, handle);
                break;
            
            case UnorderedAccessViewDimension.Texture1DArray:
                _context.Device->CreateUnorderedAccessView(null, null, new UnorderedAccessViewDesc {
                    ViewDimension = UavDimension.Texture1Darray,
                    Format = Format.FormatR32G32B32Float,
                }, handle);
                break;
            
            case UnorderedAccessViewDimension.Texture2D:
                _context.Device->CreateUnorderedAccessView(null, null, new UnorderedAccessViewDesc {
                    ViewDimension = UavDimension.Texture2D,
                    Format = Format.FormatR32G32B32Float,
                }, handle);
                break;
            
            case UnorderedAccessViewDimension.Texture2DArray:
                _context.Device->CreateUnorderedAccessView(null, null, new UnorderedAccessViewDesc {
                    ViewDimension = UavDimension.Texture2Darray,
                    Format = Format.FormatR32G32B32Float,
                }, handle);
                break;
            
            case UnorderedAccessViewDimension.Texture3D:
                _context.Device->CreateUnorderedAccessView(null, null, new UnorderedAccessViewDesc {
                    ViewDimension = UavDimension.Texture3D,
                    Format = Format.FormatR32G32B32Float,
                }, handle);
                break;
        }
    }

    public override void SetComputeConstants(uint parameterIndex, ReadOnlySpan<ConstantParameterValue> constants, uint offset) {
        EnsureNotClosed();

        fixed (ConstantParameterValue* pConstants = constants) {
            pCommandList.SetComputeRoot32BitConstants(parameterIndex, (uint)constants.Length, pConstants, offset);
        }
    }

    public override void Dispatch(uint threadGroupX, uint threadGroupY, uint threadGroupZ) {
        EnsureNotClosed();
        FlushResourceBarriers();
        
        _descCommitter.CommitCompute(pCommandList);

        pCommandList.Dispatch(threadGroupX, threadGroupY, threadGroupZ);
    }

    public override void Close() {
        if (IsClosed) return;
        
        FlushResourceBarriers();

        pCommandList.Close();
        IsClosed = true;
    }

    public override void Reset() {
        if (!IsClosed) return;

        var queue = _context.Queues.GraphicsQueue;
        queue.ReturnAllocator(queue.NextFenceValue - 1, pAllocator);
        pAllocator = queue.RequestAllocator();
        
        _uploadProvider.PreserveCurrentResource();
        _uploadProvider.ReturnResources(_context.UploadBufferPool, D3D12CommandListType.Direct, queue.NextFenceValue - 1);

        pCommandList.Reset(pAllocator, (ID3D12PipelineState*)null);
        
        IsClosed = false;
    }
    
    private UploadBufferProvider.AllocatedRegion AllocateUploadRegion(ulong size, uint alignment) {
        if (_uploadProvider.TryAllocate(size, alignment, out var region)) return region;
        
        _uploadProvider.PreserveCurrentResource();
        _uploadProvider.RequestResource(_context.UploadBufferPool, size, D3D12CommandListType.Direct, _context.Queues.GraphicsQueue.LastCompletedFenceValue);

        bool suballoc = _uploadProvider.TryAllocate(size, alignment, out region);
        Debug.Assert(suballoc, "suballoc");

        return region;
    }

    private void EnsureNotClosed() {
        if (IsClosed) throw new InvalidOperationException("Command List is being closed.");
    }

    private void FlushResourceBarriers() {
        if (_barriers.Count == 0) return;

        fixed (ResourceBarrier* pBarriers = CollectionsMarshal.AsSpan(_barriers)) {
            pCommandList.ResourceBarrier((uint)_barriers.Count, pBarriers);
        }
        _barriers.Clear();
    }
    
    protected override void Dispose() {
        IsClosed = true;
        
        _descCommitter.Dispose();

        pCommandList.Release();
        pCommandList = default;

        var queue = _context.Queues.GraphicsQueue;
        queue.ReturnAllocator(queue.NextFenceValue - 1, pAllocator);
        pAllocator = null!;
        
        _uploadProvider.PreserveCurrentResource();
        _uploadProvider.ReturnResources(_context.UploadBufferPool, D3D12CommandListType.Direct, queue.NextFenceValue - 1);
    }
}