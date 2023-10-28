namespace RiptideRendering.Direct3D12;

internal sealed unsafe class Display : IDisposable {
    public const int BufferCount = 3;

    [InlineArray(BufferCount)]
    private struct SwapchainBufferArray {
        private RenderTarget _element0;
    }

    private readonly D3D12RenderingContext _context;

    private ComPtr<IDXGISwapChain3> pSwapchain;
    private SwapchainBufferArray _swapchainBuffers;

    private DepthTexture _depthTexture = null!;

    private uint _currentSwapchainIndex;
    public uint CurrentSwapchainIndex => _currentSwapchainIndex;
    public RenderTarget CurrentSwapchainRenderTarget => _swapchainBuffers[(int)_currentSwapchainIndex];
    public DepthTexture DepthTexture => _depthTexture;

    public Display(D3D12RenderingContext context, IWindow outputWindow) {
        int hr;
        var wndSize = outputWindow.Size.As<uint>();

        _context = context;

        try {
            // Create RTV
            SwapChainDesc1 scdesc = new() {
                Width = wndSize.X,
                Height = wndSize.Y,
                AlphaMode = AlphaMode.Unspecified,
                BufferCount = BufferCount,
                BufferUsage = 1 << 5,
                Format = Format.FormatR8G8B8A8Unorm,
                SwapEffect = SwapEffect.FlipDiscard,
                SampleDesc = new() {
                    Count = 1,
                    Quality = 0,
                },
                Scaling = Scaling.None,
            };
            SwapChainFullscreenDesc scfdesc = new() {
                Windowed = true,
            };

            using ComPtr<IDXGISwapChain1> pOutputSwapchain = default;

            hr = outputWindow.CreateDxgiSwapchain(context.DxgiFactory, (IUnknown*)context.RenderingQueue.Queue, &scdesc, &scfdesc, null, pOutputSwapchain.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            using ComPtr<IDXGISwapChain3> pOutputSwapchain3 = default;
            hr = pOutputSwapchain.QueryInterface(SilkMarshal.GuidPtrOf<IDXGISwapChain3>(), (void**)pOutputSwapchain3.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            for (uint i = 0; i < BufferCount; i++) {
                _swapchainBuffers[(int)i] = new D3D12RenderTarget(_context, (IDXGISwapChain*)pOutputSwapchain.Handle, i) {
                    Name = "D3D12 Swapchain Resource " + i,
                };
            }

            // Create Depth
            _depthTexture = new D3D12DepthTexture(_context, new() {
                Width = wndSize.X,
                Height = wndSize.Y,
                Format = GraphicsFormat.D24UNormS8UInt,
                InitialStates = ResourceStates.DepthRead,
            }) {
                Name = "D3D12 Swapchain Depth Texture"
            };

            pSwapchain.Handle = pOutputSwapchain3.Detach();
        } catch {
            Dispose(true);
            throw;
        }
    }

    public void ResizeSwapchain(uint width, uint height) {
        // GPU must be idle.

        foreach (var ptr in _swapchainBuffers) ptr.DecrementReference();
        _depthTexture.DecrementReference(); _depthTexture = null!;

        _context.DestroyDeferredResources();

        _swapchainBuffers = default;

        try {
            int hr = pSwapchain.ResizeBuffers(0, width, height, Format.FormatUnknown, 0);
            Marshal.ThrowExceptionForHR(hr);

            for (uint i = 0; i < BufferCount; i++) {
                _swapchainBuffers[(int)i] = new D3D12RenderTarget(_context, (IDXGISwapChain*)pSwapchain.Handle, i) {
                    Name = "D3D12 Swapchain Resource " + i
                };
            }

            _depthTexture = new D3D12DepthTexture(_context, new() {
                Width = width,
                Height = height,
                Format = GraphicsFormat.D24UNormS8UInt,
                InitialStates = ResourceStates.DepthRead,
            }) {
                Name = "D3D12 Swapchain Depth Texture"
            };

            _currentSwapchainIndex = pSwapchain.GetCurrentBackBufferIndex();
        } catch {
            Dispose();
            throw;
        }
    }

    private void Dispose(bool disposing) {
        if (pSwapchain.Handle == null) return;

        if (disposing) { }

        _depthTexture?.DecrementReference(); _depthTexture = null!;

        for (int i = 0; i < BufferCount; i++) {
            _swapchainBuffers[i]?.DecrementReference();
        }
        _swapchainBuffers = default;

        pSwapchain.Dispose(); pSwapchain = default;
    }

    public void Present() {
        int hr = pSwapchain.Present(1, 0);
        Marshal.ThrowExceptionForHR(hr);

        _currentSwapchainIndex = pSwapchain.GetCurrentBackBufferIndex();
    }

    ~Display() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}