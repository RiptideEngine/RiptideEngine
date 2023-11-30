using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Maths;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12GraphicsCommandList : GraphicsCommandList {
    public const string UnnamedCommandList = $"<Unnamed {nameof(D3D12GraphicsCommandList)}>.{nameof(pCommandList)}";

    private readonly D3D12RenderingContext _context;

    private ComPtr<ID3D12GraphicsCommandList> pCommandList;
    private ID3D12CommandAllocator* pAllocator;

    private readonly List<ResourceBarrier> _barriers;

    private readonly DescriptorCommitter _descCommitter;

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

        pAllocator = _context.Queues.GraphicQueue.RequestAllocator();
        
        int hr = context.Device->CreateCommandList(0, CommandListType.Direct, pAllocator, null, SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList>(), (void**)pCommandList.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);
        
        Helper.SetName(pCommandList.Handle, UnnamedCommandList);

        _descCommitter = new(context);
        
        _barriers = [];
        IsClosed = false;
    }

    public override void TranslateResourceState(GpuResource resource, ResourceTranslateStates destinationStates) {
        EnsureNotClosed();
        
        CommandListOperations.AddResourceTransitionBarrier(resource, destinationStates, _barriers);
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

    public override void SetResourceSignature(ResourceSignature signature) {
        EnsureNotClosed();
        
        Debug.Assert(signature is D3D12ResourceSignature, "signature is D3D12ResourceSignature");

        var d3d12sig = Unsafe.As<D3D12ResourceSignature>(signature);

        pCommandList.SetGraphicsRootSignature(d3d12sig.RootSignature);
        _descCommitter.InitializeSignature(d3d12sig);
    }

    public override void SetPipelineState(PipelineState pipelineState) {
        EnsureNotClosed();
        
        Debug.Assert(pipelineState is D3D12PipelineState, "pipelineState is D3D12PipelineState");

        pCommandList.SetPipelineState(Unsafe.As<D3D12PipelineState>(pipelineState).PipelineState);
    }

    public override void SetGraphicsConstants(uint parameterIndex, ReadOnlySpan<uint> constants, uint offset) {
        EnsureNotClosed();

        fixed (uint* pConstants = constants) {
            pCommandList.SetGraphicsRoot32BitConstants(parameterIndex, (uint)constants.Length, pConstants, offset);
        }
    }

    public override void SetGraphicsShaderResourceView(uint parameterIndex, uint tableOffset, ShaderResourceView view) {
        EnsureNotClosed();

        Debug.Assert(view is D3D12ShaderResourceView, "view is D3D12ShaderResourceView");
        
        if (!_descCommitter.TryGetGraphicsResourceDescriptor(parameterIndex, tableOffset, out var handle)) return;
        
        FlushResourceBarriers();

        _context.Device->CopyDescriptorsSimple(1, handle, Unsafe.As<D3D12ShaderResourceView>(view).Handle, DescriptorHeapType.CbvSrvUav);
    }

    public override void Draw(uint vertexCount, uint instanceCount, uint startVertex, uint startInstance) {
        EnsureNotClosed();
        
        FlushResourceBarriers();
        
        _descCommitter.Commit(pCommandList);
        
        pCommandList.DrawInstanced(vertexCount, instanceCount, startVertex, startInstance);
    }

    public override void DrawIndexed(uint indexCount, uint instanceCount, uint startIndex, uint startInstance) {
        EnsureNotClosed();
        
        FlushResourceBarriers();

        _descCommitter.Commit(pCommandList);
        
        pCommandList.DrawIndexedInstanced(indexCount, instanceCount, startIndex, 0, startInstance);
    }

    public override void Close() {
        if (IsClosed) return;
        
        FlushResourceBarriers();

        pCommandList.Close();
        IsClosed = true;
    }

    public override void Reset() {
        if (!IsClosed) return;
        
        var queue = _context.Queues.GraphicQueue;
        queue.ReturnAllocator(queue.NextFenceValue, pAllocator);
        pAllocator = queue.RequestAllocator();

        pCommandList.Reset(pAllocator, (ID3D12PipelineState*)null);
        
        IsClosed = false;
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

        var queue = _context.Queues.GraphicQueue;
        queue.ReturnAllocator(queue.NextFenceValue, pAllocator);
        pAllocator = null!;
    }
}