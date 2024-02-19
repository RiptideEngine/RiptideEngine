using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class UploadBufferPool(D3D12RenderingContext context) : IDisposable {
    private readonly List<nint> _pool = [];

    private readonly Queue<RetiredBuffer> _retiredGraphicsBuffers = [];
    private readonly Queue<RetiredBuffer> _retiredCopyBuffers = [];
    
    private readonly List<AvailableBuffer> _availableBuffers = [];
    
    private readonly object _lock = new();

    public ID3D12Resource* Request(ulong minimumSize, D3D12CommandListType queueType, ulong fenceValue) {
        lock (_lock) {
            switch (queueType) {
                case D3D12CommandListType.Direct:
                    while (_retiredGraphicsBuffers.TryPeek(out var peek) && fenceValue >= peek.CompleteFenceValue) {
                        _availableBuffers.Add(new(peek.Resource));
                        _retiredGraphicsBuffers.Dequeue();
                    }
                    break;
                
                case D3D12CommandListType.Copy:
                    while (_retiredCopyBuffers.TryPeek(out var peek) && fenceValue >= peek.CompleteFenceValue) {
                        _availableBuffers.Add(new(peek.Resource));
                        _retiredCopyBuffers.Dequeue();
                    }
                    break;
                
                case D3D12CommandListType.Compute: throw new NotImplementedException("Unimplemented for case 'Compute'.");
            }
            
            for (int i = 0; i < _availableBuffers.Count; i++) {
                var buffer = _availableBuffers[i];

                if (buffer.Width >= minimumSize) {
                    _availableBuffers.RemoveAt(i);
                    return buffer.Resource;
                }
            }

            HeapProperties hprops = new() {
                Type = HeapType.Upload,
                CreationNodeMask = 1,
                VisibleNodeMask = 1,
            };
            ResourceDesc rdesc = new() {
                Dimension = ResourceDimension.Buffer,
                Width = minimumSize,
                Height = 1,
                DepthOrArraySize = 1,
                Alignment = 0,
                MipLevels = 1,
                SampleDesc = new() {
                    Count = 1,
                    Quality = 0,
                },
                Layout = TextureLayout.LayoutRowMajor,
            };

            ID3D12Resource* pOutput;
            HResult hr = context.Device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, ResourceStates.GenericRead, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)&pOutput);
            Marshal.ThrowExceptionForHR(hr);
            
            context.Logger?.Log(LoggingType.Info, $"Direct3D12 - {nameof(UploadBufferPool)}: Allocated {minimumSize} bytes buffer (Address = 0x{(nint)pOutput:X}).");
            
            _pool.Add((nint)pOutput);

            return pOutput;
        }
    }

    public void Return(ID3D12Resource* pResource, D3D12CommandListType queueType, ulong fenceValue) {
        lock (_lock) {
            switch (queueType) {
                case D3D12CommandListType.Direct:
                    _retiredGraphicsBuffers.Enqueue(new(pResource, fenceValue));
                    break;
                
                case D3D12CommandListType.Copy:
                    _retiredCopyBuffers.Enqueue(new(pResource, fenceValue));
                    break;
                
                case D3D12CommandListType.Compute: throw new NotImplementedException("Unimplemented case 'Compute'.");
            }
        }
    }

    public void Dispose() {
        lock (_lock) {
            foreach (var ptr in _pool) {
                ((ID3D12Resource*)ptr)->Release();
            }
            
            _pool.Clear();
            _retiredGraphicsBuffers.Clear();
            _availableBuffers.Clear();
        }
    }

    private readonly struct AvailableBuffer(ID3D12Resource* pResource) {
        public readonly ID3D12Resource* Resource = pResource;
        public readonly ulong Width = pResource->GetDesc().Width;
    }

    private readonly struct RetiredBuffer(ID3D12Resource* pResource, ulong completeFenceValue) {
        public readonly ID3D12Resource* Resource = pResource;
        public readonly ulong CompleteFenceValue = completeFenceValue;
    }
}