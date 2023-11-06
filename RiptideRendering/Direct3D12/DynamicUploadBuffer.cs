namespace RiptideRendering.Direct3D12;

internal unsafe sealed class UploadBufferStorage(D3D12RenderingContext context) {
    private readonly struct AvailableEntry(ID3D12Resource* pResource) {
        public readonly ID3D12Resource* Resource = pResource;
        public readonly ulong Size = pResource->GetDesc().Width;
    }
    private readonly struct RetiredEntry(ulong fence, ID3D12Resource* pResource) {
        public readonly ID3D12Resource* Resource = pResource;
        public readonly ulong Fence = fence;
    }

    private readonly List<nint> _pool = new();
    private readonly Queue<RetiredEntry> _retired = new();
    private readonly List<AvailableEntry> _availables = new();

    private readonly object _lock = new();
    private readonly D3D12RenderingContext _ctx = context;

    public ID3D12Resource* Request(ulong minimumSize) {
        lock (_lock) {
            while (_retired.TryPeek(out var peeked) && _ctx.RenderingQueue.IsFenceCompleted(peeked.Fence)) {
                _availables.Add(new(_retired.Dequeue().Resource));
            }

            for (int i = _availables.Count - 1; i >= 0; i--) {
                var entry = _availables[i];

                if (entry.Size >= minimumSize) {
                    _availables.RemoveAt(i);

                    return entry.Resource;
                }
            }

            ID3D12Resource* pOutput;
            HeapProperties hprops = new() {
                Type = HeapType.Upload,
                CreationNodeMask = 1,
                VisibleNodeMask = 1,
            };
            ResourceDesc rdesc = new() {
                Dimension = D3D12ResourceDimension.Buffer,
                Flags = D3D12ResourceFlags.None,
                Alignment = 0,
                Width = minimumSize,
                Height = 1,
                DepthOrArraySize = 1,
                Format = Format.FormatUnknown,
                MipLevels = 1,
                Layout = TextureLayout.LayoutRowMajor,
                SampleDesc = new() {
                    Count = 1,
                    Quality = 0,
                },
            };
            int hr = _ctx.Device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, D3D12ResourceStates.GenericRead, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)&pOutput);
            Marshal.ThrowExceptionForHR(hr);

            _pool.Add((nint)pOutput);

            Console.WriteLine($"Direct3D12 - {nameof(UploadBufferStorage)}: Resource created (Ptr = 0x{(nint)pOutput:X8}, Width = {rdesc.Width}, GPU Virtual Address = 0x{pOutput->GetGPUVirtualAddress():X8}).");

            return pOutput;
        }
    }

    public void Return(ID3D12Resource* pHeap, ulong fenceValue) {
        lock (_lock) {
            _retired.Enqueue(new(fenceValue, pHeap));
        }
    }

    public void Dispose() {
        foreach (var resource in _pool) {
            ((ID3D12Resource*)resource)->Release();
        }
        _pool.Clear();
        _retired.Clear();
        _availables.Clear();
    }
}

internal unsafe struct AllocatedUploadRegion {
    public ID3D12Resource* Resource;
    public byte* CpuAddress;
    public ulong VirtualAddress;
    public ulong Offset;
}

internal sealed unsafe class DynamicUploadBuffer(UploadBufferStorage storage) {
    private const ulong MinimumBufferSize = 1024 * 256; // 256KiB buffer seems sufficient

    private readonly List<nint> _retiredBuffers = new();

    private readonly UploadBufferStorage _storage = storage;
    private ID3D12Resource* pCurrentBuffer;
    private ulong _currentBufferGpuVirtualAddress;
    private void* _currentBufferMappedPtr;
    private ulong _currentBufferAllocatedOffset;
    private ulong _currentBufferWidth;

    public AllocatedUploadRegion Allocate(ulong size) {
        if (pCurrentBuffer == null) {
            RequestNewBuffer(size);
        } else if (_currentBufferAllocatedOffset + size >= _currentBufferWidth) {
            RetireCurrentBuffer();
            RequestNewBuffer(size);
        }

        AllocatedUploadRegion region = new() {
            Resource = pCurrentBuffer,
            CpuAddress = (byte*)_currentBufferMappedPtr + _currentBufferAllocatedOffset,
            VirtualAddress = _currentBufferGpuVirtualAddress + _currentBufferAllocatedOffset,
            Offset = _currentBufferAllocatedOffset,
        };
        _currentBufferAllocatedOffset += size;

        return region;
    }

    public AllocatedUploadRegion Allocate(ulong size, ulong alignment) {
        Debug.Assert(ulong.IsPow2(alignment), "Alignment must be power of 2.");

        if (pCurrentBuffer == null) {
            RequestNewBuffer(size);
        } else {
            var alignedOffset = _currentBufferAllocatedOffset + (alignment - 1) & ~(alignment - 1);

            if (alignedOffset + size >= _currentBufferWidth) {
                RetireCurrentBuffer();
                RequestNewBuffer(size);
            }
        }

        {
            var alignedOffset = _currentBufferAllocatedOffset + (alignment - 1) & ~(alignment - 1);

            AllocatedUploadRegion region = new() {
                Resource = pCurrentBuffer,
                CpuAddress = (byte*)_currentBufferMappedPtr + alignedOffset,
                VirtualAddress = _currentBufferGpuVirtualAddress + alignedOffset,
                Offset = alignedOffset,
            };
            _currentBufferAllocatedOffset = alignedOffset + size;

            return region;
        }
    }

    public void DeallocateLinearly(ulong size) {
        if (_currentBufferAllocatedOffset < size) return;

        _currentBufferAllocatedOffset -= size;
    }

    private void RequestNewBuffer(ulong size) {
        pCurrentBuffer = _storage.Request(ulong.Max(MinimumBufferSize, size));
        _currentBufferGpuVirtualAddress = pCurrentBuffer->GetGPUVirtualAddress();

        void* pData;
        int hr = pCurrentBuffer->Map(0, (D3D12Range*)null, &pData);
        Debug.Assert(hr >= 0);

        _currentBufferMappedPtr = pData;
        _currentBufferWidth = pCurrentBuffer->GetDesc().Width;
    }

    public void RetireCurrentBuffer() {
        if (pCurrentBuffer == null || _currentBufferAllocatedOffset == 0) return;

        pCurrentBuffer->Unmap(0, (D3D12Range*)null);

        _retiredBuffers.Add((nint)pCurrentBuffer);
        pCurrentBuffer = null;
        _currentBufferAllocatedOffset = 0;
        _currentBufferWidth = 0;
        _currentBufferMappedPtr = null;
        _currentBufferGpuVirtualAddress = 0;
    }

    public void CleanUp(ulong fenceValue) {
        RetireCurrentBuffer();

        foreach (var ptr in _retiredBuffers) {
            _storage.Return((ID3D12Resource*)ptr, fenceValue);
        }
        _retiredBuffers.Clear();
    }
}