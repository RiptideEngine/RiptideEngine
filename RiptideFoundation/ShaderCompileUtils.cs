using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using RiptideRendering.Shadering;

namespace RiptideFoundation;

public static unsafe class ShaderCompileUtils {
    public static void CompileShader(ReadOnlySpan<byte> source, ReadOnlySpan<char> target, ReadOnlySpan<char> entrypoint, ReadOnlySpan<char> optimization, DxcCompilation.Includer includer, IDxcBlob** ppOutputBytecode, IDxcBlob** ppOutputRootSignature) {
        fixed (byte* pSource = source) {
            var buffer = new Silk.NET.Direct3D.Compilers.Buffer() {
                Ptr = pSource,
                Size = (nuint)source.Length,
                Encoding = 0,
            };

            using ComPtr<IDxcCompiler3> pCompiler = default;

            int hr = DxcCompilation.CreateCompilerObject(pCompiler.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            char** pArguments = stackalloc char*[] {
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-T\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(target)),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-E\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(entrypoint)),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Zpc\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-all_resources_bound\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_rootsignature\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_debug\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_priv\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(optimization)),
            };

            using ComPtr<IDxcResult> pResult = default;
            hr = pCompiler.Compile(&buffer, pArguments, 10, includer, SilkMarshal.GuidPtrOf<IDxcResult>(), (void**)&pResult);
            Marshal.ThrowExceptionForHR(hr);

            DxcCompilation.ReportErrors<int>(pResult, (result, msg, arg) => {
                Console.WriteLine("Shader compilation warning: " + msg);
            }, 0, (result, msg, arg) => {
                throw new Exception("Failed to compile Shader: " + msg);
            }, 0);

            if (ppOutputRootSignature != null) {
                hr = pResult.GetOutput(OutKind.RootSignature, SilkMarshal.GuidPtrOf<IDxcBlob>(), (void**)ppOutputRootSignature, null);
                Marshal.ThrowExceptionForHR(hr);
            }

            hr = pResult.GetResult(ppOutputBytecode);
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}