namespace RiptideRendering.Direct3D12;

internal sealed unsafe partial class D3D12RenderingContext : BaseRenderingContext {
    private D3D12Factory _factory;
    private D3D12CapabilityChecker _capCheck;

    public D3D12 D3D12 { get; private set; }
    public DXGI DXGI { get; private set; }

    private ComPtr<ID3D12Device4> pDevice;
    private ComPtr<IDXGIFactory2> pFactory;

    internal Display Display { get; private set; }

    private DeviceDependentConstants _devdepConsts;
    internal ref readonly DeviceDependentConstants Constants => ref _devdepConsts;
    internal Debugger? Debugger { get; private set; }
    public ID3D12Device4* Device => pDevice;
    public IDXGIFactory2* DxgiFactory => pFactory;
    internal CommandQueue RenderingQueue { get; private set; }

    private readonly DeferredDestructor _deferredDestructor;

    public override RenderingAPI RenderingAPI => RenderingAPI.Direct3D12;
    public override BaseFactory Factory => _factory;
    public override BaseCapabilityChecker CapabilityChecker => _capCheck;

    public override (GpuResource Resource, RenderTargetView View) SwapchainCurrentRenderTarget => Display.CurrentSwapchainRenderTarget;

    public override ILoggingService? Logger { get; set; }

    public D3D12RenderingContext(ContextOptions options) {
        int hr;

        try {
            D3D12 = D3D12.GetApi();
            DXGI = DXGI.GetApi(options.OutputWindow, false);

            bool debug = options.OutputWindow.API.Flags.HasFlag(ContextFlags.Debug);

            using ComPtr<IDXGIFactory2> pOutputFactory = default;
            hr = DXGI.CreateDXGIFactory2(debug ? 0x01U : 0x00, SilkMarshal.GuidPtrOf<IDXGIFactory2>(), (void**)&pOutputFactory);
            Marshal.ThrowExceptionForHR(hr);

            if (debug) {
                using ComPtr<ID3D12Debug> pDebug = default;
                if (D3D12.GetDebugInterface(SilkMarshal.GuidPtrOf<ID3D12Debug>(), (void**)&pDebug) >= 0) {
                    pDebug.EnableDebugLayer();

                    using ComPtr<ID3D12Debug1> pDebug1 = default;
                    if (pDebug.QueryInterface(SilkMarshal.GuidPtrOf<ID3D12Debug1>(), (void**)&pDebug1) >= 0) {
                        pDebug1.SetEnableGPUBasedValidation(true);
                    }
                }
            }

            using ComPtr<IDXGIAdapter1> pAdapter = default;
            DxgiHelper.GetHardwareAdapter(D3D12, (IDXGIFactory1*)pOutputFactory.Handle, true, pAdapter.GetAddressOf());

            using ComPtr<ID3D12Device> pOutputDevice = default;
            hr = D3D12.CreateDevice((IUnknown*)pAdapter.Handle, D3DFeatureLevel.Level121, SilkMarshal.GuidPtrOf<ID3D12Device>(), (void**)&pOutputDevice);
            Marshal.ThrowExceptionForHR(hr);

            hr = pOutputDevice.QueryInterface(SilkMarshal.GuidPtrOf<ID3D12Device4>(), (void**)pDevice.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            _devdepConsts = new((ID3D12Device*)pDevice.Handle);
            _capCheck = new(this);

            pFactory.Handle = pOutputFactory.Detach();

            if (debug) {
                Debugger = new(this);
            }

            RenderingQueue = new(this, CommandListType.Direct);
            _factory = new(this);

            RootSigStorage = new(this);
            UploadBufferPool = new(this);

            StagingResourceHeapPool = new(this, DescriptorHeapType.CbvSrvUav);
            StagingSamplerHeapPool = new(this, DescriptorHeapType.Sampler);

            GpuResourceDescHeapPool = new(this, DescriptorHeapType.CbvSrvUav, 8192);
            CurrentResourceGpuDescHeap = new(GpuResourceDescHeapPool, _devdepConsts.ResourceViewDescIncrementSize);

            GpuSamplerDescHeapPool = new(this, DescriptorHeapType.Sampler, 1024);
            CurrentSamplerGpuDescHeap = new(GpuSamplerDescHeapPool, _devdepConsts.SamplerDescIncrementSize);

            CommandListPool = new(this);

            _resourceDescAlloc = [
                new(this, DescriptorHeapType.CbvSrvUav),
                new(this, DescriptorHeapType.Sampler),
                new(this, DescriptorHeapType.Rtv),
                new(this, DescriptorHeapType.Dsv),
            ];

            Display = new(this, options.OutputWindow);

            _deferredDestructor = new();
        } catch {
            Dispose();
            throw;
        }
    }

    public void AddToDeferredDestruction(ID3D12Resource* pResource) {
        _deferredDestructor.QueueResource(RenderingQueue.NextFenceValue - 1, pResource);
    }

    public void DestroyDeferredResources() {
        _deferredDestructor.ReleaseResources(RenderingQueue.CompletedValue);
    }

    protected override void Dispose(bool disposing) {
        if (D3D12 == null) return;
        RenderingQueue.WaitForIdle();

        if (disposing) {
            CommandListPool?.Dispose(); CommandListPool = null!;
            RootSigStorage?.Dispose(); RootSigStorage = null!;
            Display?.Dispose(); Display = null!;
            UploadBufferPool?.Dispose(); UploadBufferPool = null!;
            RenderingQueue?.Dispose(); RenderingQueue = null!;

            GpuResourceDescHeapPool?.Dispose(); GpuResourceDescHeapPool = null!;
            CurrentResourceGpuDescHeap = null!;

            GpuSamplerDescHeapPool?.Dispose(); GpuSamplerDescHeapPool = null!;
            CurrentSamplerGpuDescHeap = null!;

            StagingResourceHeapPool?.Dispose(); StagingResourceHeapPool = null!;
            StagingSamplerHeapPool?.Dispose(); StagingSamplerHeapPool = null!;

            _deferredDestructor?.Dispose();

            foreach (var allocator in _resourceDescAlloc) allocator.Dispose();
            _resourceDescAlloc = [];

            if (Debugger != null) {
                Debugger.ReportLiveD3D12Objects();
                Debugger.Dispose();
            }
            Debugger = null!;
        }

        pDevice.Dispose(); pDevice = default;
        pFactory.Dispose(); pFactory = default;

        D3D12?.Dispose(); D3D12 = null!;
        DXGI?.Dispose(); DXGI = null!;
        _factory = null!; _capCheck = null!;

        _devdepConsts = default;
    }

    ~D3D12RenderingContext() {
        Dispose(disposing: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CpuDescriptorAllocator GetResourceDescriptorAllocator(DescriptorHeapType type) => _resourceDescAlloc[(int)type];
}