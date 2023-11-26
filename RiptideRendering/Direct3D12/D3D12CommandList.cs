namespace RiptideRendering.Direct3D12;

internal unsafe partial class D3D12CommandList : CommandList {
    private const string UnnamedCommandList = $"<Unnamed {nameof(D3D12CommandList)}>.{nameof(pCommandList)}";

    private ComPtr<ID3D12GraphicsCommandList> pCommandList;
    private ID3D12CommandAllocator* pAllocator;
    private readonly D3D12RenderingContext _context;

    private readonly SuballocateUploadBuffer _uploadBuffer;
    private readonly DescriptorCommitter _descCommitter;

    private readonly List<ResourceBarrier> _barriers;

    public ID3D12GraphicsCommandList* CommandList => pCommandList;

    public override string? Name {
        get => base.Name;
        set {
            base.Name = value;

            if (pCommandList.Handle != null) {
                if (value == null) {
                    D3D12Helper.SetName(pCommandList.Handle, UnnamedCommandList);
                } else {
                    var reqLength = value.Length + 1 + nameof(pCommandList).Length;
                    var charArray = ArrayPool<char>.Shared.Rent(reqLength);
                    {
                        value.CopyTo(charArray);
                        charArray[value.Length] = '.';
                        nameof(pCommandList).CopyTo(0, charArray, value.Length + 1, nameof(pCommandList).Length);

                        D3D12Helper.SetName(pCommandList.Handle, charArray.AsSpan(0, reqLength));
                    }
                    ArrayPool<char>.Shared.Return(charArray);
                }
            }
        }
    }

    public D3D12CommandList(D3D12RenderingContext context) {
        _context = context;

        pAllocator = context.RenderingQueue.RequestAllocator();

        ID3D12GraphicsCommandList* pOutputList;
        int hr = context.Device->CreateCommandList(1, CommandListType.Direct, pAllocator, null, SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList>(), (void**)&pOutputList);
        Marshal.ThrowExceptionForHR(hr);

        D3D12Helper.SetName(pOutputList, UnnamedCommandList);

        pCommandList.Handle = pOutputList;

        _descCommitter = new(context);
        _barriers = new();
        _uploadBuffer = new(context.UploadBufferPool);

        _refcount = 1;
    }

    public override void SetStencilRef(uint stencilRef) {
        EnsureNotClosed();

        pCommandList.OMSetStencilRef(stencilRef);
    }

    public override void SetViewport(Rectangle2D area) {
        EnsureNotClosed();

        Unsafe.SkipInit(out D3D12Viewport vp);

        Unsafe.Write(&vp.TopLeftX, area);
        Unsafe.Write(&vp.MinDepth, Vector2.UnitY);

        pCommandList.RSSetViewports(1, &vp);
    }

    public override void SetScissorRect(Bound2DInt area) {
        EnsureNotClosed();
        pCommandList.RSSetScissorRects(1, (Silk.NET.Maths.Box2D<int>*)&area);
    }

    public override void TranslateState(GpuResource resource, ResourceTranslateStates newStates) {
        EnsureNotClosed();
        
        Debug.Assert(resource is D3D12GpuBuffer or D3D12GpuTexture, "resource is D3D12GpuBuffer or D3D12GpuTexture");
        
        ResourceStates newStates2 = D3D12Convert.Convert(newStates);
        
        if (resource is D3D12GpuBuffer d3d12buffer) {
            if (d3d12buffer.UsageState != newStates2) {
                ResourceBarrier barrier = new() {
                    Type = ResourceBarrierType.Transition,
                    Transition = new() {
                        PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                        Subresource = 0xFFFFFFFF,
                        StateBefore = d3d12buffer.UsageState,
                        StateAfter = newStates2,
                    },
                };

                if (newStates2 == d3d12buffer.TransitioningState) {
                    barrier.Flags = ResourceBarrierFlags.EndOnly;
                    d3d12buffer.TransitioningState = (ResourceStates)(-1);
                }

                d3d12buffer.UsageState = newStates2;
                _barriers.Add(barrier);
            } else if (newStates2 == ResourceStates.UnorderedAccess) {
                ResourceBarrier barrier = new() {
                    Type = ResourceBarrierType.Uav,
                    UAV = new() {
                        PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                    },
                };
                
                _barriers.Add(barrier);
            }
        } else {
            var d3d12texture = Unsafe.As<D3D12GpuTexture>(resource);

            if (d3d12texture.UsageState != newStates2) {
                ResourceBarrier barrier = new() {
                    Type = ResourceBarrierType.Transition,
                    Transition = new() {
                        PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                        Subresource = 0xFFFFFFFF,
                        StateBefore = d3d12texture.UsageState,
                        StateAfter = newStates2,
                    },
                };

                if (newStates2 == d3d12texture.TransitioningState) {
                    barrier.Flags = ResourceBarrierFlags.EndOnly;
                    d3d12texture.TransitioningState = (ResourceStates)(-1);
                }

                d3d12texture.UsageState = newStates2;
                _barriers.Add(barrier);
            } else if (newStates2 == ResourceStates.UnorderedAccess) {
                ResourceBarrier barrier = new() {
                    Type = ResourceBarrierType.Uav,
                    UAV = new() {
                        PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                    },
                };
                
                _barriers.Add(barrier);
            }
        }
    }

    public override void SetRenderTarget(RenderTargetView renderTarget, DepthStencilView? depthView) {
        Debug.Assert(renderTarget is D3D12RenderTargetView, "renderTarget is D3D12RenderTargetView");
        
        EnsureNotClosed();
        FlushResourceBarriers();

        var rtvHandle = Unsafe.As<D3D12RenderTargetView>(renderTarget).Handle;

        if (depthView != null) {
            Debug.Assert(depthView is D3D12DepthStencilView, "depthView is D3D12DepthStencilView");

            CpuDescriptorHandle depthHandle = Unsafe.As<D3D12DepthStencilView>(depthView).Handle;
            pCommandList.OMSetRenderTargets(1, rtvHandle, false, &depthHandle);
        } else {
            pCommandList.OMSetRenderTargets(1, rtvHandle, false, null);
        }
    }

    public override void ClearDepthTexture(DepthStencilView view, DepthClearFlags flags, float depth, byte stencil, ReadOnlySpan<Bound2DInt> clearAreas) {
        Debug.Assert(view is D3D12DepthStencilView, "view is D3D12DepthStencilView");
        
        EnsureNotClosed();
        FlushResourceBarriers();
        
        fixed (Bound2DInt* pAreas = clearAreas) {
            pCommandList.ClearDepthStencilView(Unsafe.As<D3D12DepthStencilView>(view).Handle, (ClearFlags)flags, depth, stencil, (uint)clearAreas.Length, (Silk.NET.Maths.Box2D<int>*)pAreas);
        }
    }

    public override void ClearRenderTarget(RenderTargetView view, Color color, ReadOnlySpan<Bound2DInt> clearAreas) {
        Debug.Assert(view is D3D12RenderTargetView, "view is D3D12RenderTargetView");
        
        EnsureNotClosed();
        FlushResourceBarriers();
        
        fixed (Bound2DInt* pAreas = clearAreas) {
            pCommandList.ClearRenderTargetView(Unsafe.As<D3D12RenderTargetView>(view).Handle, &color.R, (uint)clearAreas.Length, (Silk.NET.Maths.Box2D<int>*)pAreas);
        }
    }
    
    public override void Draw(uint vertexCount, uint instanceCount, uint startVertexLoc, uint startInstanceLoc) {
        EnsureNotClosed();
        FlushResourceBarriers();
        
        _descCommitter.CommitGraphics(pCommandList);

        pCommandList.DrawInstanced(vertexCount, instanceCount, startVertexLoc, startInstanceLoc);
    }

    public override void DrawIndexed(uint indexCount, uint instanceCount, uint startIndexLoc, uint startInstanceLoc) {
        EnsureNotClosed();
        FlushResourceBarriers();

        _descCommitter.CommitGraphics(pCommandList);
        pCommandList.DrawIndexedInstanced(indexCount, instanceCount, startIndexLoc, 0, startInstanceLoc);
    }

    public override void Close() {
        if (IsClosed) return;
        
        FlushResourceBarriers();

        int hr = pCommandList.Close();
        Marshal.ThrowExceptionForHR(hr);

        IsClosed = true;
    }

    private void FlushResourceBarriers() {
        if (_barriers.Count == 0) return;
        
        pCommandList.ResourceBarrier((uint)_barriers.Count, CollectionsMarshal.AsSpan(_barriers));
        _barriers.Clear();
    }

    public AllocatedUploadRegion ReserveUploadRegion(ulong size) => _uploadBuffer.Allocate(size);
    public AllocatedUploadRegion ReserveUploadRegion(ulong size, ulong alignment) => _uploadBuffer.Allocate(size, alignment);

    public void Reinitialize() {
        Debug.Assert(pAllocator == null);

        pAllocator = _context.RenderingQueue.RequestAllocator();
        pCommandList.Reset(pAllocator, (ID3D12PipelineState*)null);

        IsClosed = false;

        var fence = _context.RenderingQueue.CompletedValue;

        _uploadBuffer.CleanUp(fence);
        _descCommitter.CleanUp(fence);

        base.Name = null;
        D3D12Helper.SetName((ID3D12Object*)pCommandList.Handle, UnnamedCommandList);
        _barriers.Clear();

        _refcount = 1;
    }

    public void TrueDispose() {
        pCommandList.Dispose(); pCommandList = null;

        if (pAllocator != null) {
            _context.RenderingQueue.ReturnAllocator(_context.RenderingQueue.NextFenceValue - 1, pAllocator);
            pAllocator = null;
        }

        _descCommitter.Dispose(_context.RenderingQueue.NextFenceValue - 1);
    }

    protected override void Dispose() {
        if (!IsClosed) {
            pCommandList.Close();
        }

        if (pAllocator != null) {
            _context.RenderingQueue.ReturnAllocator(_context.RenderingQueue.NextFenceValue - 1, pAllocator);
            pAllocator = null;
        }

        _descCommitter.Dispose(_context.RenderingQueue.NextFenceValue - 1);
        _context.CommandListPool.Return(this);
    }
}