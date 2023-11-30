using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12CopyCommandList : CopyCommandList {
    public const string UnnamedCommandList = $"<Unnamed {nameof(D3D12CopyCommandList)}>.{nameof(pCommandList)}";
    
    private readonly D3D12RenderingContext _context;

    private ComPtr<ID3D12GraphicsCommandList> pCommandList;
    private ID3D12CommandAllocator* pAllocator;

    private readonly List<ResourceBarrier> _barriers;
    private readonly UploadBufferProvider _uploadProvider;
    
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
    
    public D3D12CopyCommandList(D3D12RenderingContext context) {
        _context = context;

        var queue = _context.Queues.CopyQueue;

        pAllocator = queue.RequestAllocator();
        int hr = context.Device->CreateCommandList(0, CommandListType.Copy, pAllocator, null, SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList>(), (void**)pCommandList.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);
        
        Helper.SetName(pCommandList.Handle, UnnamedCommandList);

        _barriers = [];
        IsClosed = false;

        _uploadProvider = new();
        _uploadProvider.RequestResource(context.UploadBufferPool, D3D12.DefaultResourcePlacementAlignment, queue.LastCompletedFenceValue);
    }
    
    public override void TranslateResourceState(GpuResource resource, ResourceTranslateStates destinationStates) {
        EnsureNotClosed();
        
        CommandListOperations.AddResourceTransitionBarrier(resource, destinationStates, _barriers);
    }

    public override void UpdateBuffer(GpuBuffer dest, ReadOnlySpan<byte> data) {
        EnsureNotClosed();
        
        Debug.Assert(dest is D3D12GpuBuffer, "dest is D3D12GpuBuffer");
        
        ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuBuffer>(dest).NativeResourceHandle;
        ResourceDesc rdesc = pResource->GetDesc();

        if ((ulong)data.Length < rdesc.Width) throw new ArgumentException($"Not enough data to update buffer, {rdesc.Width} bytes expected, but only {data.Length} bytes provided.");

        var uploadRegion = AllocateUploadRegion(rdesc.Width, 1);
        
        data.CopyTo(new(uploadRegion.Pointer, (int)rdesc.Width));
        
        pCommandList.CopyBufferRegion(pResource, 0, uploadRegion.Resource, uploadRegion.Offset, rdesc.Width);
    }

    public override void UpdateTexture(GpuTexture dest, uint subresource, ReadOnlySpan<byte> data) {
        EnsureNotClosed();
        
        Debug.Assert(dest is D3D12GpuTexture, "dest is D3D12GpuTexture");
        
        ID3D12Resource* pResource = (ID3D12Resource*)Unsafe.As<D3D12GpuTexture>(dest).NativeResourceHandle;
        ResourceDesc rdesc = pResource->GetDesc();

        if (subresource >= rdesc.MipLevels) return;

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
        
        TextureCopyLocation destination = new() {
            Type = TextureCopyType.SubresourceIndex,
            PResource = pResource,
            SubresourceIndex = 0,
        };
        TextureCopyLocation source = new() {
            Type = TextureCopyType.PlacedFootprint,
            PResource = uploadRegion.Resource,
            PlacedFootprint = footprint,
        };
        
        pCommandList.CopyTextureRegion(&destination, 0, 0, 0, &source, (Box*)null);
    }

    private UploadBufferProvider.AllocatedRegion AllocateUploadRegion(ulong size, uint alignment) {
        if (_uploadProvider.TryAllocate(size, alignment, out var region)) return region;
        
        _uploadProvider.PreserveCurrentResource();
        _uploadProvider.RequestResource(_context.UploadBufferPool, size, _context.Queues.CopyQueue.LastCompletedFenceValue);

        bool suballoc = _uploadProvider.TryAllocate(size, alignment, out region);
        Debug.Assert(suballoc, "suballoc");

        return region;
    }

    public override void Close() {
        if (IsClosed) return;
        
        FlushResourceBarriers();

        _uploadProvider.PreserveCurrentResource();
        
        pCommandList.Close();
        IsClosed = true;
    }

    public override void Reset() {
        if (!IsClosed) return;
        
        var queue = _context.Queues.CopyQueue;
        queue.ReturnAllocator(queue.NextFenceValue - 1, pAllocator);
        pAllocator = queue.RequestAllocator();
        
        pCommandList.Reset(pAllocator, (ID3D12PipelineState*)null);
        _uploadProvider.ReturnResources(_context.UploadBufferPool, queue.NextFenceValue - 1);
        
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

        pCommandList.Release();
        pCommandList = default;

        var queue = _context.Queues.CopyQueue;
        queue.ReturnAllocator(queue.NextFenceValue, pAllocator);
        pAllocator = null!;
    }
}