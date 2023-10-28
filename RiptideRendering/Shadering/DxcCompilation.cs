namespace RiptideRendering.Shadering;

public static unsafe partial class DxcCompilation {
    // TODO: Per thread compiler object?

    public struct Includer : IDisposable {
        private nint* _vtable;

        private readonly Delegate _loadSrcDel;
        private IEnumerable<string> _includes;

        public Includer(IEnumerable<string> includes) {
            _vtable = (nint*)NativeMemory.Alloc((nuint)nint.Size, 5);
            _includes = includes;

            _vtable[0] = (nint)(_vtable + 1);
            _vtable[1] = 0;
            _vtable[2] = (nint)(delegate* unmanaged<IDxcIncludeHandler*, uint>)&AddRef;
            _vtable[3] = (nint)(delegate* unmanaged<IDxcIncludeHandler*, uint>)&Release;
            _vtable[4] = Marshal.GetFunctionPointerForDelegate<Delegate>(_loadSrcDel = LoadSource);
        }

        [UnmanagedCallersOnly] private static uint AddRef(IDxcIncludeHandler* pThis) => 0;
        [UnmanagedCallersOnly] private static uint Release(IDxcIncludeHandler* pThis) => 0;

        private int LoadSource(IDxcIncludeHandler* pThis, char* pFilename, IDxcBlob** ppIncludeSource) {
            foreach (var include in _includes) {
                string combined = Path.Combine(include, Marshal.PtrToStringUni((nint)pFilename)!);

                IDxcBlobEncoding* pBlob;
                int hr = Utils->LoadFile(combined, null, &pBlob);

                if (hr >= 0) {
                    *ppIncludeSource = (IDxcBlob*)pBlob;
                    return hr;
                }
            }

            *ppIncludeSource = null;
            return unchecked((int)0x80004005);
        }

        public static implicit operator IDxcIncludeHandler*(Includer includer) => (IDxcIncludeHandler*)includer._vtable;

        public void Dispose() {
            if (_vtable == null) return;

            NativeMemory.Free(_vtable); _vtable = null;
            _includes = Array.Empty<string>();
        }
    }

    public static readonly string StandardLanguageVersion = "2021\0";

    public static DXC Dxc { get; private set; } = null!;

    private static ComPtr<IDxcUtils> pUtils;

    public static IDxcUtils* Utils => pUtils;

    private static nint _dxil;
    private static uint _initCounter;

    public static void Initialize() {
        if (Interlocked.Increment(ref _initCounter) != 1) return;

        _dxil = NativeLibrary.Load("dxil.dll");
        Dxc = DXC.GetApi();

        int hr;
        Guid CLSID_DxcUtils = new(0x6245d6af, 0x66e0, 0x48fd, 0x80, 0xb4, 0x4d, 0x27, 0x17, 0x96, 0x74, 0x8c);

        try {
            _dxil = NativeLibrary.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dxil.dll"));

            hr = Dxc.CreateInstance(&CLSID_DxcUtils, SilkMarshal.GuidPtrOf<IDxcUtils>(), (void**)pUtils.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);
        } catch {
            Dispose();
            throw;
        }
    }

    public static void Shutdown() {
        if (Interlocked.Decrement(ref _initCounter) != 0) return;

        Dispose();
    }

    private static void Dispose() {
        NativeLibrary.Free(_dxil); _dxil = nint.Zero;
        pUtils.Dispose(); pUtils = default;

        Dxc?.Dispose(); Dxc = null!;
    }

    public static void AssertInitialized() {
        Debug.Assert(_dxil != nint.Zero, "DxcCompilation API hasn't initialized yet.");
    }

    public static int CreateCompilerObject(IDxcCompiler3** ppOutput) {
        Guid CLSID_DxcCompiler = new(0x73e22d93, 0xe6ce, 0x47f3, 0xb5, 0xbf, 0xf0, 0x66, 0x4f, 0x39, 0xc1, 0xb0);
        return Dxc.CreateInstance(&CLSID_DxcCompiler, SilkMarshal.GuidPtrOf<IDxcCompiler3>(), (void**)ppOutput);
    }

    public static int CreateValidator(IDxcValidator2** ppOutput) {
        Guid CLSID_DxcValidator = new(0x8ca3e215, 0xf728, 0x4cf3, 0x8c, 0xdd, 0x88, 0xaf, 0x91, 0x75, 0x87, 0xa1);
        return Dxc.CreateInstance(&CLSID_DxcValidator, SilkMarshal.GuidPtrOf<IDxcValidator2>(), (void**)ppOutput);
    }

    public delegate void MessageReporter<T>(IDxcResult* pResult, string message, T? arg);
    public static void ReportErrors<T>(IDxcResult* pResult, MessageReporter<T>? warningReporter, T? warningArg, MessageReporter<T>? errorReporter, T? errorArg) {
        if (pResult == null) return;
        if (warningReporter == null && errorReporter == null) return;

        using ComPtr<IDxcBlobUtf8> pError = default;
        if (pResult->GetOutput(OutKind.Errors, SilkMarshal.GuidPtrOf<IDxcBlobUtf8>(), (void**)pError.GetAddressOf(), null) >= 0 && pError.GetBufferPointer() != null && pError.GetBufferSize() > 0) {
            int status;
            pResult->GetStatus(&status);

            var msg = Marshal.PtrToStringAnsi((nint)pError.GetBufferPointer(), (int)pError.GetBufferSize());
            if (status < 0) {
                errorReporter?.Invoke(pResult, msg, errorArg);
            } else {
                warningReporter?.Invoke(pResult, msg, warningArg);
            }
        }
    }

    public static void ReportErrors<T, U>(IDxcResult* pResult, MessageReporter<T>? warningReporter, T? warningArg, MessageReporter<U>? errorReporter, U? errorArg) {
        if (pResult == null) return;
        if (warningReporter == null && errorReporter == null) return;

        using ComPtr<IDxcBlobUtf8> pError = default;
        if (pResult->GetOutput(OutKind.Errors, SilkMarshal.GuidPtrOf<IDxcBlobUtf8>(), (void**)pError.GetAddressOf(), null) >= 0 && pError.GetBufferPointer() != null && pError.GetBufferSize() > 0) {
            int status;
            pResult->GetStatus(&status);

            var msg = Marshal.PtrToStringAnsi((nint)pError.GetBufferPointer(), (int)pError.GetBufferSize());
            if (status < 0) {
                errorReporter?.Invoke(pResult, msg, errorArg);
            } else {
                warningReporter?.Invoke(pResult, msg, warningArg);
            }
        }
    }
}