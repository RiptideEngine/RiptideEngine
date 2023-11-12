using RPCSToolkit;

namespace RiptideRendering.Direct3D12;

internal unsafe partial class D3D12CommandList : CommandList {
    private const string UnnamedCommandList = $"<Unnamed {nameof(D3D12CommandList)}>.{nameof(pCommandList)}";

    private ComPtr<ID3D12GraphicsCommandList> pCommandList;
    private ID3D12CommandAllocator* pAllocator;
    private readonly D3D12RenderingContext _context;

    private readonly SuballocateUploadBuffer _uploadBuffer;
    private readonly DescriptorCommitter _descCommitter;

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

        _uploadBuffer = new(context.UploadBufferPool);

        _refcount = 1;
    }

    public override void SetStencilRef(uint stencilRef) {
        EnsureNotClosed();

        pCommandList.OMSetStencilRef(stencilRef);
    }

    public override void SetViewport(Rectangle<float> area) {
        EnsureNotClosed();

        Unsafe.SkipInit(out D3D12Viewport vp);

        Unsafe.Write(&vp.TopLeftX, area);
        Unsafe.Write(&vp.MinDepth, Vector2.UnitY);

        pCommandList.RSSetViewports(1, &vp);
    }

    public override void SetScissorRect(Bound2D<int> area) {
        EnsureNotClosed();
        pCommandList.RSSetScissorRects(1, (Silk.NET.Maths.Box2D<int>*)&area);
    }

    public override void TranslateResourceStates(ReadOnlySpan<ResourceTransitionDescriptor> descs) {
        EnsureNotClosed();

        List<ResourceBarrier> barriers = ListPool<ResourceBarrier>.Shared.Get();

        try {
            for (int i = 0; i < descs.Length; i++) {
                ref readonly var desc = ref descs[i];

                barriers.Add(new() {
                    Type = ResourceBarrierType.Transition,
                    Transition = new() {
                        PResource = (ID3D12Resource*)desc.Target.Handle,
                        Subresource = uint.MaxValue,
                        StateBefore = D3D12Convert.Convert(desc.OldStates),
                        StateAfter = D3D12Convert.Convert(desc.NewStates),
                    },
                });

                if (desc.NewStates.HasFlag(ResourceStates.UnorderedAccess)) {
                    barriers.Add(new() {
                        Type = ResourceBarrierType.Uav,
                        UAV = new() {
                            PResource = (ID3D12Resource*)desc.Target.Handle,
                        },
                    });
                }
            }

            pCommandList.ResourceBarrier((uint)barriers.Count, CollectionsMarshal.AsSpan(barriers));
        } finally {
            ListPool<ResourceBarrier>.Shared.Return(barriers);
        }
    }

    public override void SetRenderTarget(NativeRenderTargetView renderTarget) {
        EnsureNotClosed();
        CpuDescriptorHandle handle = new() { Ptr = (nuint)renderTarget.Handle, };
        pCommandList.OMSetRenderTargets(1, &handle, false, (CpuDescriptorHandle*)null);
    }

    public override void SetRenderTarget(NativeRenderTargetView renderTarget, NativeDepthStencilView depthTexture) {
        EnsureNotClosed();
        var rtv = Unsafe.BitCast<ulong, CpuDescriptorHandle>(renderTarget.Handle);

        if (depthTexture.Handle != 0) {
            CpuDescriptorHandle depthHandle = new() { Ptr = (nuint)depthTexture.Handle };
            pCommandList.OMSetRenderTargets(1, rtv, false, &depthHandle);
        } else {
            pCommandList.OMSetRenderTargets(1, rtv, false, null);
        }
    }

    public override void ClearDepthTexture(NativeDepthStencilView handle, DepthClearFlags flags, float depth, byte stencil) {
        EnsureNotClosed();
        pCommandList.ClearDepthStencilView(new() { Ptr = (nuint)handle.Handle, }, (ClearFlags)flags, depth, stencil, 0, (Silk.NET.Maths.Box2D<int>*)null);
    }

    public override void ClearDepthTexture(NativeDepthStencilView handle, DepthClearFlags flags, float depth, byte stencil, ReadOnlySpan<Bound2D<int>> clearAreas) {
        EnsureNotClosed();
        fixed (Bound2D<int>* pAreas = clearAreas) {
            pCommandList.ClearDepthStencilView(new() { Ptr = (nuint)handle.Handle }, (ClearFlags)flags, depth, stencil, (uint)clearAreas.Length, (Silk.NET.Maths.Box2D<int>*)pAreas);
        }
    }

    public override void ClearRenderTarget(NativeRenderTargetView handle, Color color) {
        EnsureNotClosed();
        pCommandList.ClearRenderTargetView(new() { Ptr = (nuint)handle.Handle }, &color.R, 0, (Silk.NET.Maths.Box2D<int>*)null);
    }

    public override void ClearRenderTarget(NativeRenderTargetView handle, Color color, ReadOnlySpan<Bound2D<int>> clearAreas) {
        EnsureNotClosed();
        fixed (Bound2D<int>* pBoxes = clearAreas) {
            pCommandList.ClearRenderTargetView(new() { Ptr = (nuint)handle.Handle }, &color.R, (uint)clearAreas.Length, (Silk.NET.Maths.Box2D<int>*)pBoxes);
        }
    }

    public override void Close() {
        if (IsClosed) return;

        int hr = pCommandList.Close();
        Marshal.ThrowExceptionForHR(hr);

        IsClosed = true;
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