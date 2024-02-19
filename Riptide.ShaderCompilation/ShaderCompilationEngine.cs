using Silk.NET.SPIRV.Cross;
using Silk.NET.SPIRV.Reflect;

namespace Riptide.ShaderCompilation;

public static unsafe partial class ShaderCompilationEngine {
    private static bool _initialized;

    // API objects.
    internal static DXC Dxc { get; private set; } = null!;
    internal static Cross SpirvCross { get; private set; } = null!;
    internal static Reflect SpirvReflect { get; private set; } = null!;
    
    // Others.
    private static ComPtr<IDxcUtils> pDxcUtils;
    internal static IDxcUtils* DxcUtils => pDxcUtils;
    
    public static void Initialize() {
        if (_initialized) throw new InvalidOperationException("Already initialized.");

        Dxc = DXC.GetApi();
        SpirvCross = Cross.GetApi();
        SpirvReflect = Reflect.GetApi();
        
        Guid CLSID_DxcUtils = new(0x6245d6af, 0x66e0, 0x48fd, 0x80, 0xb4, 0x4d, 0x27, 0x17, 0x96, 0x74, 0x8c);
        
        pDxcUtils = Dxc.CreateInstance<IDxcUtils>(ref CLSID_DxcUtils);

        _initialized = true;
    }
    public static void Shutdown() {
        if (!_initialized) return;

        pDxcUtils.Dispose();
        pDxcUtils = default;
        
        Dxc.Dispose(); Dxc = null!;
        SpirvCross.Dispose(); SpirvCross = null!;
        SpirvReflect.Dispose(); SpirvReflect = null!;
        
        _initialized = false;
    }

    public static void AssertInitialized() {
        Debug.Assert(_initialized, "Shader Compilation Engine is not initialized.");
    }

    public static void CreateDxcCompiler3(IDxcCompiler3** ppOutput) {
        AssertInitialized();
        
        Guid CLSID_DxcCompiler = new(0x73e22d93, 0xe6ce, 0x47f3, 0xb5, 0xbf, 0xf0, 0x66, 0x4f, 0x39, 0xc1, 0xb0);

        int hr = Dxc.CreateInstance(ref CLSID_DxcCompiler, SilkMarshal.GuidPtrOf<IDxcCompiler3>(), (void**)ppOutput);
        Marshal.ThrowExceptionForHR(hr);
    }
}