using Silk.NET.SPIRV;
using Buffer = Silk.NET.Direct3D.Compilers.Buffer;
using Silk.NET.SPIRV.Cross;

namespace Riptide.ShaderCompilation;

unsafe partial class D3D12CompilationPipeline {
    public override CompiledResult Compile(ReadOnlySpan<byte> source) {
        fixed (byte* pSource = source) {
            Buffer buffer = new() {
                Ptr = pSource,
                Size = (nuint)source.Length,
                Encoding = 0,
            };

            LoadSourceDelegate _ldsrc = LoadSource;
            
            nint* vtable = stackalloc nint[5];
            vtable[0] = (nint)(&vtable[1]);
            vtable[1] = 0;
            vtable[2] = vtable[3] = (nint)(delegate* unmanaged[Stdcall]<IDxcIncludeHandler*, uint>)(&AddRefOrRelease);
            vtable[4] = (nint)(delegate* unmanaged[Stdcall]<IDxcIncludeHandler*, char*, IDxcBlob**, HResult>)Marshal.GetFunctionPointerForDelegate(_ldsrc);

            var result = CompileFromHlsl(buffer, _optimize, _entrypoint, _target, (IDxcIncludeHandler*)vtable);

            GC.KeepAlive(_ldsrc);
            
            return result;
        }
    }
    
    private static DxcCompiledResult CompileFromHlsl(Buffer source, OptimizationLevel optimizationLevel, string entrypoint, string target, IDxcIncludeHandler* pIncludeHandler) {
        int hr;
        
        using ComPtr<IDxcCompilerArgs> pArguments = default;
        fixed (char* pEntrypoint = entrypoint) {
            fixed (char* pTarget = target) {
                hr = ShaderCompilationEngine.DxcUtils->BuildArguments((char*)null, pEntrypoint, pTarget, (char**)null, 0, null, 0, pArguments.GetAddressOf());
                Marshal.ThrowExceptionForHR(hr);
                
                pArguments.AddArguments((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>(optimizationLevel switch {
                    OptimizationLevel.Disable => "-Od\0",
                    OptimizationLevel.Level0 => "-O0\0",
                    OptimizationLevel.Level1 => "-O1\0",
                    OptimizationLevel.Level2 => "-O2\0",
                    OptimizationLevel.Level3 => "-O3\0",
                    _ => throw new UnreachableException(),
                })), 1);

                pArguments.AddArguments((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_reflect\0")), 1);
                pArguments.AddArguments((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_rootsignature\0")), 1);
                pArguments.AddArguments((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_debug\0")), 1);
                pArguments.AddArguments((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Zpc\0")), 1);
                // Current DXC version should default set -HV to 2021, so no need to add it.
            }
        }
            
        using ComPtr<IDxcCompiler3> pCompiler = default;
        ShaderCompilationEngine.CreateDxcCompiler3(pCompiler.GetAddressOf());

        using ComPtr<IDxcResult> pResult = default;
        hr = pCompiler.Compile(&source, pArguments.GetArguments(), pArguments.GetCount(), pIncludeHandler, SilkMarshal.GuidPtrOf<IDxcResult>(), (void**)pResult.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        int status;
        hr = pResult.GetStatus(&status);
        Marshal.ThrowExceptionForHR(hr);

        if (status < 0) {
            // Fail
            using ComPtr<IDxcBlobEncoding> pError = default;
            hr = pResult.GetErrorBuffer(pError.GetAddressOf());
            Debug.Assert(hr >= 0, "hr >= 0");

            Bool32 known;
            uint codepage;

            hr = pError.GetEncoding((int*)&known, &codepage);
            Debug.Assert(hr >= 0, "hr >= 0");

            string error = Encoding.GetEncoding(known ? (int)codepage : 0).GetString((byte*)pError.GetBufferPointer(), (int)pError.GetBufferSize());

            throw new ShaderCompileException($"Shader compilation failed: \"{error}\".");
        }

        IDxcBlob* pObject, pReflection;
        hr = pResult.GetOutput(OutKind.Object, SilkMarshal.GuidPtrOf<IDxcBlob>(), (void**)&pObject, null);
        Debug.Assert(hr >= 0, "hr >= 0");
        
        hr = pResult.GetOutput(OutKind.Reflection, SilkMarshal.GuidPtrOf<IDxcBlob>(), (void**)&pReflection, null);
        Debug.Assert(hr >= 0, "hr >= 0");

        return new(new(pObject), new(pReflection));
    }
}