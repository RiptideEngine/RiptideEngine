using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using DxcBuffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace Riptide.ShaderCompilation;

public sealed unsafe class DxcCompilationPipeline : CompilationPipeline {
    private string _optimization;
    private string? _entrypoint;
    private string? _target;

    private bool _spirv = false;
    
    public static DXC Dxc { get; private set; } = null!;
    private static IDxcUtils* pUtils;
    private static nint _dxil;

    public DxcCompilationPipeline() {
        if (pUtils == null) {
            _dxil = NativeLibrary.Load("dxil.dll");
            Dxc = DXC.GetApi();

            int hr;
            Guid CLSID_DxcUtils = new(0x6245d6af, 0x66e0, 0x48fd, 0x80, 0xb4, 0x4d, 0x27, 0x17, 0x96, 0x74, 0x8c);

            try {
                _dxil = NativeLibrary.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dxil.dll"));

                fixed (IDxcUtils** ppUtils = &pUtils) {
                    hr = Dxc.CreateInstance(&CLSID_DxcUtils, SilkMarshal.GuidPtrOf<IDxcUtils>(), (void**)ppUtils);
                    Marshal.ThrowExceptionForHR(hr);
                }
            } catch {
                Dispose();
                throw;
            }
        } else {
            pUtils->AddRef();
        }
        
        _optimization = "-O3\0";
        _entrypoint = _target = string.Empty;
    }

    public override CompilationPipeline ResetDefault() {
        _spirv = false;
        _optimization = "-O3\0";
        _entrypoint = _target = string.Empty;
        
        return this;
    }

    public override CompilationPipeline SetOptimizationLevel(OptimizationLevel optimize) {
        _optimization = optimize switch {
            OptimizationLevel.Level0 => "-O0\0",
            OptimizationLevel.Level1 => "-O1\0",
            OptimizationLevel.Level2 => "-O2\0",
            OptimizationLevel.Level3 => "-O3\0",
            _ => "-Od\0",
        };

        return this;
    }

    protected override CompilationPipeline SetEntrypointImpl(ReadOnlySpan<char> entrypoint) {
        _entrypoint = $"{entrypoint}\0";
        return this;
    }

    protected override CompilationPipeline SetTargetImpl(ReadOnlySpan<char> target) {
        _target = $"{target}\0";
        return this;
    }

    public override CompilationPipeline SetSpecializedArgument(ReadOnlySpan<char> key, ReadOnlySpan<char> value, bool silent = true) {
        switch (key) {
            case SpecializedArguments.OutputFormat:
                switch (value) {
                    case "dxil": _spirv = false; return this;
                    case "spirv": _spirv = true; return this;
                    default:
                        if (!silent) throw new ArgumentException("Output format can only have value of 'dxil' or 'spirv'.", nameof(value));
                        return this;
                }
                
            default:
                if (!silent) throw new ArgumentException($"Invalid specialize argument key provided, key must be one of these following values: '{SpecializedArguments.OutputFormat}'.");
                return this;
        }
    }

    public override CompiledResult Compile(ReadOnlySpan<byte> sourceCode) {
        if (_target == null) throw new InvalidOperationException("Unknown compilation target.");
        if (_entrypoint == null) throw new InvalidOperationException("Unknown entrypoint name.");
        
        fixed (byte* pSource = sourceCode) {
            DxcBuffer buffer = new() {
                Ptr = pSource,
                Size = (nuint)sourceCode.Length,
                Encoding = 0,
            };

            char** pArguments = stackalloc char*[10];

            pArguments[0] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-T\0"));
            pArguments[1] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>(_target));
            pArguments[2] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-E\0"));
            pArguments[3] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>(_entrypoint));
            pArguments[4] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Zpc\0"));
            pArguments[5] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_rootsignature\0"));
            pArguments[6] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_debug\0"));
            pArguments[7] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_priv\0"));
            pArguments[7] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_reflect\0"));
            pArguments[8] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>(_optimization));

            if (_spirv) {
                pArguments[9] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-spirv"));
            }

            // Creating the compiler now can help us handling multi-thread situation.
            using ComPtr<IDxcCompiler3> pCompiler = default;
            int hr = CreateCompilerObject(pCompiler.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            using ComPtr<IDxcResult> pResult = default;
            hr = pCompiler.Compile(&buffer, pArguments, 9U + Unsafe.BitCast<bool, byte>(_spirv), (IDxcIncludeHandler*)null, SilkMarshal.GuidPtrOf<IDxcResult>(), (void**)&pResult);
            Marshal.ThrowExceptionForHR(hr);

            DxcCompiledPayload? errorPayload;
            
            using ComPtr<IDxcBlobUtf8> pError = default;
            if (pResult.GetOutput(OutKind.Errors, SilkMarshal.GuidPtrOf<IDxcBlob>(), (void**)pError.GetAddressOf(), null) >= 0 && pError.GetBufferPointer() != null && pError.GetBufferSize() > 0) {
                pResult.GetStatus(&hr);
                errorPayload = new DxcCompiledPayload((IDxcBlob*)pError.Handle);

                if (hr < 0) {
                    return new(errorPayload, false, null, null);
                }
            } else {
                errorPayload = null;
            }

            using ComPtr<IDxcBlob> pShader = default;
            using ComPtr<IDxcBlob> pReflection = default;

            pResult.GetOutput(OutKind.Object, SilkMarshal.GuidPtrOf<IDxcBlob>(), (void**)pShader.GetAddressOf(), null);
            pResult.GetOutput(OutKind.Reflection, SilkMarshal.GuidPtrOf<IDxcBlob>(), (void**)pReflection.GetAddressOf(), null);
            
            return new(null, true, new DxcCompiledPayload(pShader), new DxcCompiledPayload(pReflection));
        }
    }
    
    private static int CreateCompilerObject(IDxcCompiler3** ppOutput) {
        Guid CLSID_DxcCompiler = new(0x73e22d93, 0xe6ce, 0x47f3, 0xb5, 0xbf, 0xf0, 0x66, 0x4f, 0x39, 0xc1, 0xb0);
        return Dxc.CreateInstance(&CLSID_DxcCompiler, SilkMarshal.GuidPtrOf<IDxcCompiler3>(), (void**)ppOutput);
    }

    public override bool IsTargetSupported(ReadOnlySpan<char> target) {
        int major = target[target.IndexOf('_') + 1] - '0';
        int minor = target[^1] - '0';

        return major switch {
            5 => minor == 1,
            6 => minor is >= 0 and <= 7,
            _ => false,
        };
    }

    protected override void DisposeImpl(bool disposing) {
        if (pUtils->Release() == 0) {
            Dxc.Dispose();
            Dxc = null!;
            pUtils = null;
            NativeLibrary.Free(_dxil);
        }
    }
}