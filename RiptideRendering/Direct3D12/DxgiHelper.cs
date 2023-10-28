namespace RiptideRendering;

internal static unsafe class DxgiHelper {
    public static void GetHardwareAdapter(D3D12 api, IDXGIFactory1* pFactory, bool highPerformance, IDXGIAdapter1** ppOutputAdapter) {
        using ComPtr<IDXGIFactory6> pFactory6 = default;
        if (pFactory->QueryInterface(SilkMarshal.GuidPtrOf<IDXGIFactory6>(), (void**)&pFactory6) >= 0) {
            GpuPreference preference = highPerformance ? GpuPreference.HighPerformance : GpuPreference.Unspecified;
            for (uint i = 0; pFactory6.EnumAdapterByGpuPreference(i, preference, SilkMarshal.GuidPtrOf<IDXGIAdapter1>(), (void**)ppOutputAdapter) >= 0; i++) {
                AdapterDesc1 desc;
                (*ppOutputAdapter)->GetDesc1(&desc);

                if (((AdapterFlag)desc.Flags).HasFlag(AdapterFlag.Software)) goto release;

                if (api.CreateDevice((IUnknown*)*ppOutputAdapter, D3DFeatureLevel.Level110, SilkMarshal.GuidPtrOf<ID3D12Device>(), null) >= 0) break;

                release:
                (*ppOutputAdapter)->Release(); *ppOutputAdapter = null;
            }
        } else {
            for (uint i = 0; pFactory->EnumAdapters1(i, ppOutputAdapter) >= 0; i++) {
                AdapterDesc1 desc;
                (*ppOutputAdapter)->GetDesc1(&desc);

                if (((AdapterFlag)desc.Flags).HasFlag(AdapterFlag.Software)) goto release;

                if (api.CreateDevice((IUnknown*)*ppOutputAdapter, D3DFeatureLevel.Level110, SilkMarshal.GuidPtrOf<ID3D12Device>(), null) >= 0) break;

                release:
                (*ppOutputAdapter)->Release(); *ppOutputAdapter = null;
            }
        }
    }
}