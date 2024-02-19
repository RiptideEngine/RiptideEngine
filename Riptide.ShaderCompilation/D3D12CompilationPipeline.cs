namespace Riptide.ShaderCompilation;

public sealed unsafe partial class D3D12CompilationPipeline : CompilationPipeline {
    private string _entrypoint = "main\0";
    private string _target = "vs_6_0\0";
    private OptimizationLevel _optimize = OptimizationLevel.Level1;
    private IncludePathTransformer? _transformer;

    public D3D12CompilationPipeline() {
        ShaderCompilationEngine.AssertInitialized();
    }

    private HResult LoadSource(IDxcIncludeHandler* pThis, char* pFilename, IDxcBlob** ppOutput) {
        if (_transformer == null) {
            *ppOutput = null!;
            return unchecked((int)0x80004005);
        }
        
        var path = _transformer.Transform(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pFilename));

        fixed (char* pFile = $"{path}\0") {
            return ShaderCompilationEngine.DxcUtils->LoadFile(pFile, null, (IDxcBlobEncoding**)ppOutput);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })] private static uint AddRefOrRelease(IDxcIncludeHandler* pThis) => 0;
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate HResult LoadSourceDelegate(IDxcIncludeHandler* pThis, char* pFilename, IDxcBlob** ppOutput);
    
    private sealed class DxcCompiledResult : CompiledResult {
        private DxilCompiledBytecode _shader;
        private DxilReflectInformation _reflection;

        public DxcCompiledResult(DxilCompiledBytecode shader, DxilReflectInformation reflection) {
            _shader = shader;
            _reflection = reflection;

            _refcount = 1;
        }
        
        public override CompiledBytecode GetShaderBytecode() => _shader;
        public override ReflectInformation GetReflectionInfo() => _reflection;

        protected override void Dispose() {
            _shader?.DecrementReference();
            _shader = null!;

            _reflection?.DecrementReference();
            _reflection = null!;
        }
    }
    private sealed class DxilCompiledBytecode : CompiledBytecode {
        private IDxcBlob* pBlob;
        
        public DxilCompiledBytecode(IDxcBlob* pBlob) {
            this.pBlob = pBlob;
            _refcount = 1;
        }
        
        public override ReadOnlySpan<byte> GetData() {
            return new(pBlob->GetBufferPointer(), (int)pBlob->GetBufferSize());
        }
        protected override void Dispose() {
            pBlob->Release();
            pBlob = null;
        }
    }
    internal sealed class DxilReflectInformation : ReflectInformation {
        private IDxcBlob* pBlob;
        public IDxcBlob* Blob => pBlob;
        
        public DxilReflectInformation(IDxcBlob* pBlob) {
            this.pBlob = pBlob;
            _refcount = 1;
        }
        
        protected override void Dispose() {
            pBlob->Release();
            pBlob = null;
        }
    }
}