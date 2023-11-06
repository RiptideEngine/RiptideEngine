namespace RiptideRendering.Direct3D12;

internal sealed unsafe class Display : IDisposable {
    public const int BufferCount = 3;

    [InlineArray(BufferCount)]
    private struct SwapchainBufferArray {
        private (D3D12GpuResource Resource, D3D12RenderTargetView View) _element0;
    }

    private readonly D3D12RenderingContext _context;

    private ComPtr<IDXGISwapChain3> pSwapchain;
    private SwapchainBufferArray _swapchainBuffers;

    private uint _currentSwapchainIndex;
    public uint CurrentSwapchainIndex => _currentSwapchainIndex;
    public (GpuResource Resource, RenderTargetView View) CurrentSwapchainRenderTarget => _swapchainBuffers[(int)_currentSwapchainIndex];

    public Display(D3D12RenderingContext context, IWindow outputWindow) {
        int hr;
        var wndSize = outputWindow.Size.As<uint>();

        _context = context;

        try {
            // Create Swapchain
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

            pSwapchain.Handle = pOutputSwapchain3.Detach();

            CreateRenderTargets();
        } catch {
            Dispose(true);
            throw;
        }
    }

    public void ResizeSwapchain(uint width, uint height) {
        // GPU must be idle.

        foreach ((var texture, var view) in _swapchainBuffers) {
            texture?.DecrementReference();
            view?.DecrementReference();
        }
        _swapchainBuffers = default;

        _context.DestroyDeferredResources();

        _swapchainBuffers = default;

        try {
            int hr = pSwapchain.ResizeBuffers(0, width, height, Format.FormatUnknown, 0);
            Marshal.ThrowExceptionForHR(hr);

            CreateRenderTargets();

            _currentSwapchainIndex = pSwapchain.GetCurrentBackBufferIndex();
        } catch {
            Dispose();
            throw;
        }
    }

    public void Present() {
        int hr = pSwapchain.Present(1, 0);
        Marshal.ThrowExceptionForHR(hr);

        _currentSwapchainIndex = pSwapchain.GetCurrentBackBufferIndex();
    }

    private void CreateRenderTargets() {
        RenderTargetViewDescriptor desc = new() {
            Dimension = RenderTargetViewDimension.Texture2D,
            Format = GraphicsFormat.R8G8B8A8UNorm,
            Texture2D = new() {
                MipSlice = 0,
                PlaneSlice = 0,
            },
        };

        for (uint i = 0; i < BufferCount; i++) {
            var texture = new D3D12GpuResource(_context, (IDXGISwapChain*)pSwapchain.Handle, i) {
                Name = "D3D12 Swapchain Resource " + i,
            };
            var view = new D3D12RenderTargetView(_context, texture, desc);

            _swapchainBuffers[(int)i] = (texture, view);
        }
    }

    private void Dispose(bool disposing) {
        if (pSwapchain.Handle == null) return;

        if (disposing) { }

        foreach ((var texture, var view) in _swapchainBuffers) {
            texture?.DecrementReference();
            view?.DecrementReference();
        }
        _swapchainBuffers = default;

        pSwapchain.Dispose(); pSwapchain = default;
    }


    ~Display() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}