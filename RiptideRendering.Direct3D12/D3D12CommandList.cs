namespace RiptideRendering.Direct3D12;

internal unsafe partial class D3D12CommandList : CommandList {
    private const string UnnamedCommandList = $"<Unnamed {nameof(D3D12CommandList)}>.{nameof(pCommandList)}";

    private ComPtr<ID3D12GraphicsCommandList> pCommandList;
    private ID3D12CommandAllocator* pAllocator;
    private readonly D3D12RenderingContext _context;
    private D3D12GraphicalShader? _graphicalShader;
    private D3D12ComputeShader? _computeShader;

    private readonly DynamicUploadBuffer _uploadBuffer;
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

        _uploadBuffer = new(context.UploadBufferStorage);

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
        _barriers.EnsureCapacity(descs.Length);

        foreach (ref readonly var desc in descs) {
            var oldStates = D3D12Convert.Convert(desc.OldStates);
            var newStates = D3D12Convert.Convert(desc.NewStates);

            if (oldStates != newStates) {
                ResourceBarrier barrier = new() {
                    Type = ResourceBarrierType.Transition,
                    Transition = new() {
                        PResource = (ID3D12Resource*)desc.Target.ResourceHandle,
                        Subresource = uint.MaxValue,
                        StateBefore = oldStates,
                        StateAfter = newStates,
                    },
                };
                _barriers.Add(barrier);
            } else if (desc.NewStates == ResourceStates.UnorderedAccess) {
                _barriers.Add(new() {
                    Type = ResourceBarrierType.Uav,
                    UAV = new() {
                        PResource = (ID3D12Resource*)desc.Target.ResourceHandle,
                    },
                });
            }
        }

        if (_barriers.Count == 0) return;

        pCommandList.ResourceBarrier((uint)_barriers.Count, (ResourceBarrier*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(_barriers))));
        _barriers.Clear();
    }

    public override void SetRenderTarget(RenderTargetHandle renderTarget) {
        EnsureNotClosed();
        CpuDescriptorHandle handle = new() { Ptr = (nuint)renderTarget.Handle, };
        pCommandList.OMSetRenderTargets(1, &handle, false, (CpuDescriptorHandle*)null);
    }

    public override void SetRenderTarget(RenderTargetHandle renderTarget, DepthStencilHandle? depthTexture) {
        EnsureNotClosed();
        var rtv = Unsafe.BitCast<ulong, CpuDescriptorHandle>(renderTarget.Handle);

        if (depthTexture.HasValue) {
            CpuDescriptorHandle depthHandle = new() { Ptr = (nuint)depthTexture.Value.Handle };
            pCommandList.OMSetRenderTargets(1, rtv, false, &depthHandle);
        } else {
            pCommandList.OMSetRenderTargets(1, rtv, false, null);
        }
    }

    public override void ClearDepthTexture(DepthStencilHandle handle, DepthTextureClearFlags flags, float depth, byte stencil) {
        EnsureNotClosed();
        pCommandList.ClearDepthStencilView(new() { Ptr = (nuint)handle.Handle, }, (ClearFlags)flags, depth, stencil, 0, (Silk.NET.Maths.Box2D<int>*)null);
    }

    public override void ClearDepthTexture(DepthStencilHandle handle, DepthTextureClearFlags flags, float depth, byte stencil, ReadOnlySpan<Bound2D<int>> clearAreas) {
        EnsureNotClosed();
        fixed (Bound2D<int>* pAreas = clearAreas) {
            pCommandList.ClearDepthStencilView(new() { Ptr = (nuint)handle.Handle }, (ClearFlags)flags, depth, stencil, (uint)clearAreas.Length, (Silk.NET.Maths.Box2D<int>*)pAreas);
        }
    }

    public override void ClearRenderTarget(RenderTargetHandle handle, Color color) {
        EnsureNotClosed();
        pCommandList.ClearRenderTargetView(new() { Ptr = (nuint)handle.Handle }, &color.R, 0, (Silk.NET.Maths.Box2D<int>*)null);
    }

    public override void ClearRenderTarget(RenderTargetHandle handle, Color color, ReadOnlySpan<Bound2D<int>> clearAreas) {
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

        _graphicalShader?.DecrementReference(); _graphicalShader = null;
        _computeShader?.DecrementReference(); _computeShader = null;
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

        _graphicalShader?.DecrementReference(); _graphicalShader = null;
        _computeShader?.DecrementReference(); _computeShader = null;

        _descCommitter.Dispose(_context.RenderingQueue.NextFenceValue - 1);
        _context.CommandListPool.Return(this);
    }
}