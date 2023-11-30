using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using System.Collections.Concurrent;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe partial class D3D12RenderingContext : RenderingContext {
    public override RenderingAPI RenderingAPI => RenderingAPI.Direct3D12;

    private readonly D3D12Factory _factory;
    private readonly D3D12CapabilityChecker _capChecker;
    private DXGI _dxgi;

    private ComPtr<ID3D12Device4> pDevice;
    private ComPtr<IDXGIFactory6> pDxgiFactory;

    private readonly ConcurrentQueue<nint> _deferredDestroyResource = [];

    public override Factory Factory => _factory;
    public override CapabilityChecker CapabilityChecker => _capChecker;
    
    public override ILoggingService? Logger { get; set; }
    
    public D3DFeatureLevel FeatureLevel { get; private set; }

    public CommandQueues Queues { get; }

    public ID3D12Device4* Device => pDevice.Handle;

    public D3D12 D3D12 { get; }

    public D3D12RenderingContext(ContextOptions options) {
        D3D12 = D3D12.GetApi();
        _dxgi = DXGI.GetApi(options.OutputWindow);

        HResult hr;
        
        try {
            pDxgiFactory = _dxgi.CreateDXGIFactory2<IDXGIFactory6>(0x01);
            
            InitializeDebugLayer();
            
            // Device creation
            {
                Span<D3DFeatureLevel> featureLevels = [
                    D3DFeatureLevel.Level122,
                    D3DFeatureLevel.Level121,
                    D3DFeatureLevel.Level120,
                    D3DFeatureLevel.Level111,
                    D3DFeatureLevel.Level110,
                ];

                D3DFeatureLevel level = default;

                IDXGIAdapter1* pAdapter;
                for (uint i = 0; pDxgiFactory.EnumAdapterByGpuPreference(i, GpuPreference.HighPerformance, SilkMarshal.GuidPtrOf<IDXGIAdapter1>(), (void**)&pAdapter) >= 0; i++) {
                    AdapterDesc1 desc;
                    if (pAdapter->GetDesc1(&desc) < 0 || desc.Flags == 2) {
                        pAdapter->Release();
                        pAdapter = null;
                        continue;
                    }

                    foreach (var createLevel in featureLevels) {
                        hr = D3D12.CreateDevice((IUnknown*)pAdapter, createLevel, SilkMarshal.GuidPtrOf<ID3D12Device>(), null);

                        if (hr.IsSuccess) {
                            level = createLevel;
                            goto foundAdapter;
                        }
                    }

                    pAdapter->Release();
                    pAdapter = null;
                }

                foundAdapter:
                if (pAdapter == null) throw new PlatformNotSupportedException("Cannot find an adapter suitable to create ID3D12Device.");

                using ComPtr<ID3D12Device> outputDevice = default;

                hr = D3D12.CreateDevice((IUnknown*)pAdapter, level, SilkMarshal.GuidPtrOf<ID3D12Device>(), (void**)outputDevice.GetAddressOf());
                pAdapter->Release();
                Marshal.ThrowExceptionForHR(hr);

                hr = outputDevice.QueryInterface(SilkMarshal.GuidPtrOf<ID3D12Device4>(), (void**)pDevice.GetAddressOf());
                if (hr.IsError) throw new PlatformNotSupportedException("Failed to QueryInterface ID3D12Device into ID3D12Device4.");

                FeatureLevel = level;
            }
            
            InitializeDebugMessageCallback();

            Queues = new(this);
            
            InitializeResources();
            InitializeDisplay(options.OutputWindow);
        } catch {
            Dispose();
            throw;
        }

        _factory = new(this);
        _capChecker = new(this);
    }

    public override void WaitQueueIdle(QueueType type) {
        switch (type) {
            case QueueType.Graphics: Queues.GraphicQueue.WaitForIdle(); break;
            case QueueType.Compute: Queues.ComputeQueue.WaitForIdle(); break;
            case QueueType.Copy: Queues.CopyQueue.WaitForIdle(); break;
        }
    }

    public override bool WaitFence(ulong fenceValue) {
        var queue = Queues.GetQueue(fenceValue);
        return queue.WaitForFence(fenceValue);
    }

    public override ulong ExecuteCommandList(GraphicsCommandList commandList) {
        if (!commandList.IsClosed) throw new InvalidOperationException("Command List must be closed before executing.");
        
        Debug.Assert(commandList is D3D12GraphicsCommandList, "commandList is D3D12GraphicsCommandList");
        
        return Queues.GraphicQueue.ExecuteCommandList((ID3D12CommandList*)Unsafe.As<D3D12GraphicsCommandList>(commandList).CommandList);
    }

    public override ulong ExecuteCommandList(CopyCommandList commandList) {
        if (!commandList.IsClosed) throw new InvalidOperationException("Command List must be closed before executing.");

        Debug.Assert(commandList is D3D12CopyCommandList, "commandList is D3D12CopyCommandList");

        ulong fence = Queues.CopyQueue.ExecuteCommandList((ID3D12CommandList*)Unsafe.As<D3D12CopyCommandList>(commandList).CommandList);
        return fence;
    }

    public void DestroyDeferred(ID3D12Resource* resource) {
        _deferredDestroyResource.Enqueue((nint)resource);
    }

    protected override void Dispose(bool disposing) {
        Queues?.Dispose();

        foreach (var resource in _deferredDestroyResource) {
            ((ID3D12Resource*)resource)->Release();
        }

        DisposeDisplay();
        DisposeResources();
        
        ShutdownDebugLayer();
        
        pDxgiFactory.Release(); pDxgiFactory = null!;
        pDevice.Release(); pDevice = null!;

        D3D12.Dispose();
        _dxgi.Dispose();
    }
}