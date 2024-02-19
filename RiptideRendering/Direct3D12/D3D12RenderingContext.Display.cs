using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Feature = Silk.NET.Direct3D12.Feature;

namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12RenderingContext {
    public const int BufferCount = 3;
    
    private ComPtr<IDXGISwapChain3> pSwapchain;
    
    private PerFrameData[] _perFrameDatas = [];
    
    private uint _swapchainBackBufferIndex;

    public override (GpuResource Resource, RenderTargetView View) SwapchainCurrentRenderTarget => (_perFrameDatas[_swapchainBackBufferIndex].BackBuffer, _perFrameDatas[_swapchainBackBufferIndex].View);

    private void InitializeDisplay(IWindow outputWindow, GraphicsFormat format) {
        HResult hr;
        
        if (!Converting.TryConvert(format, out var dxgiFormat)) {
            dxgiFormat = Format.FormatR8G8B8A8Unorm;
        } else {
            FeatureDataFormatSupport info = new() {
                Format = dxgiFormat,
            };
            hr = Device->CheckFeatureSupport(Feature.FormatSupport, &info, (uint)sizeof(FeatureDataFormatSupport));
            Debug.Assert(hr.IsSuccess, "hr.IsSuccess");

            if ((info.Support1 & FormatSupport1.Display) != FormatSupport1.Display) {
                dxgiFormat = Format.FormatR8G8B8A8Unorm;
            }
        }
        
        SwapChainDesc1 scdesc = new() {
            Width = (uint)outputWindow.Size.X,
            Height = (uint)outputWindow.Size.Y,
            AlphaMode = AlphaMode.Unspecified,
            BufferCount = BufferCount,
            BufferUsage = 1 << 5,
            Format = dxgiFormat,
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

        try {
            using ComPtr<IDXGISwapChain1> outputSwapchain = default;
            hr = outputWindow.CreateDxgiSwapchain((IDXGIFactory2*)pDxgiFactory.Handle, (IUnknown*)Queues.GraphicsQueue.Queue, &scdesc, &scfdesc, null, outputSwapchain.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            pSwapchain = outputSwapchain.QueryInterface<IDXGISwapChain3>();

            _perFrameDatas = new PerFrameData[BufferCount];
            for (uint i = 0; i < BufferCount; i++) {
                D3D12GpuTexture texture = new(this, (IDXGISwapChain*)pSwapchain.Handle, i);
                CpuDescriptorHandle rtvHandle = AllocateCpuDescriptor(DescriptorHeapType.Rtv);
                
                pDevice.CreateRenderTargetView((ID3D12Resource*)texture.NativeResourceHandle, (RenderTargetViewDesc*)null, rtvHandle);
                
                _perFrameDatas[i] = new() {
                    BackBuffer = texture,
                    View = new(rtvHandle),
                    PresentFenceValue = 0,
                };
            }
        } catch {
            DisposeDisplay();
            throw;
        }

        _swapchainBackBufferIndex = pSwapchain.GetCurrentBackBufferIndex();
    }

    private void DisposeDisplay() {
        foreach (ref var data in _perFrameDatas.AsSpan()) {
            data.Dispose();
        }
        _perFrameDatas = [];
        
        pSwapchain.Release();
    }
    
    protected override void ResizeSwapchainImpl(uint width, uint height) {
    }
    
    public override void Present() {
        var queue = Queues.GraphicsQueue;
        uint swapchainBufferIndex = _swapchainBackBufferIndex;
        
        int hr = pSwapchain.Present(1, 0);
        Marshal.ThrowExceptionForHR(hr);
        
        _swapchainBackBufferIndex = pSwapchain.GetCurrentBackBufferIndex();

        ref var data = ref _perFrameDatas[swapchainBufferIndex];
        queue.WaitForFence(data.PresentFenceValue);
        data.PresentFenceValue = queue.NextFenceValue - 1;
        
        _deferredDestructor.Destroy(queue.NextFenceValue - 1);
    }

    private struct PerFrameData : IDisposable {
        public D3D12GpuTexture BackBuffer;
        public D3D12RenderTargetView View;
        public ulong PresentFenceValue;

        public void Dispose() {
            View?.DecrementReference();
            BackBuffer?.DecrementReference();
        }
    }
}