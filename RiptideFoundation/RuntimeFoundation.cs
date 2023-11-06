using RiptideRendering.Shadering;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace RiptideFoundation;

internal static unsafe class RuntimeFoundation {
    internal static IRenderingService RenderingService { get; private set; } = null!;
    internal static ISceneGraphService SceneGraphService { get; private set; } = null!;
    internal static IRuntimeWindowService WindowService { get; private set; } = null!;
    internal static IInputService InputService { get; private set; } = null!;
    internal static IComponentDatabase ComponentDatabase { get; private set; } = null!;
    internal static ResourceDatabase ResourceDatabase { get; private set; } = null!;

    internal static ResourceSignature TestPipelineResource { get; private set; } = null!;
    internal static PipelineState TestPipelineState { get; private set; } = null!;

    internal static void AssertInitialized() {
        Debug.Assert(RenderingService != null, $"{nameof(RuntimeFoundation)} hasn't registered it's services to use yet. Have you called {nameof(RuntimeFoundation)}.{nameof(Initialize)}?");
    }

    public static void Initialize(RiptideServices services) {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        if (RenderingService != null) throw new InvalidOperationException("A service has already been used to initialize RiptideFoundation.");

        RenderingService = services.GetRequiredService<IRenderingService>();
        SceneGraphService = services.GetRequiredService<ISceneGraphService>();
        WindowService = services.GetRequiredService<IRuntimeWindowService>();
        InputService = services.GetRequiredService<IInputService>();
        ComponentDatabase = services.GetRequiredService<IComponentDatabase>();
        ResourceDatabase = (ResourceDatabase)services.GetRequiredService<IResourceDatabase>();

        CreateTestPipelineState();
    }

    public static void Shutdown() {
        TestPipelineState?.DecrementReference();
    }

    private static void CreateTestPipelineState() {
        using ComPtr<IDxcBlob> pVSBlob = default;
        using ComPtr<IDxcBlob> pPSBlob = default;

        ReadOnlySpan<byte> source = @"
#include ""Vertex.hlsl""
#include ""Camera.hlsl""
#include ""Transformation.hlsl""

struct VSInput {
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct Vertex {
    float3 position : Position;
    uint color : Color;
};

struct PSInput {
    float4 sv_position : SV_Position;
    float4 color : Color;
};

RIPTILE_DECLARE_VERTEX_BUFFER(Vertex, 0);

PSInput vsmain(VSInput i) {
    Vertex v = GET_VERTEX_DATA(0, i.vid);
    PSInput o;

    o.sv_position = mul(_Transformation.MVP, float4(v.position, 1));
    o.color = float4(1, 1, 1, 1);

    return o;
};

float4 psmain(PSInput i) : SV_Target {
    return i.color;
};"u8;

        fixed (byte* pSource = source) {
            using var includer = new DxcCompilation.Includer(new string[] {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ShaderAPI"),
            });

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
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("vs_6_0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-E\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("vsmain")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Zpc\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-all_resources_bound\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_rootsignature\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_debug\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-Qstrip_priv\0")),
                (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("-O0")),
            };

            using ComPtr<IDxcResult> pResult = default;
            hr = pCompiler.Compile(&buffer, pArguments, 10, includer, SilkMarshal.GuidPtrOf<IDxcResult>(), (void**)&pResult);
            Marshal.ThrowExceptionForHR(hr);

            DxcCompilation.ReportErrors<int>(pResult, (result, msg, arg) => {
                Console.WriteLine("Vertex Shader compilation warning: " + msg);
            }, 0, (result, msg, arg) => {
                throw new Exception("Failed to compile Vertex Shader: " + msg);
            }, 0);

            hr = pResult.GetResult(pVSBlob.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            pArguments[1] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("ps_6_0"));
            pArguments[3] = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<char>("psmain"));

            pResult.Dispose();

            hr = pCompiler.Compile(&buffer, pArguments, 10, includer, SilkMarshal.GuidPtrOf<IDxcResult>(), (void**)&pResult);
            Marshal.ThrowExceptionForHR(hr);

            DxcCompilation.ReportErrors<int>(pResult, (result, msg, arg) => {
                Console.WriteLine("Pixel Shader compilation warning: " + msg);
            }, 0, (result, msg, arg) => {
                throw new Exception("Failed to compile Pixel Shader: " + msg);
            }, 0);

            hr = pResult.GetResult(pPSBlob.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);
        }

        var rdesc = new RasterizationConfig() {
            FillMode = FillingMode.Solid,
            CullMode = CullingMode.None,
            Conservative = false,
        };
        var depth = new DepthStencilConfig() {
            EnableDepth = false,
            EnableStencil = false,
            DepthComparison = ComparisonOperator.Always,
            FrontFaceOperation = new() {
                FailOp = StencilOperation.Keep,
                DepthFailOp = StencilOperation.Keep,
                PassOp = StencilOperation.Keep,
                CompareOp = ComparisonOperator.Always,
            },
            BackfaceOperation = new() {
                FailOp = StencilOperation.Keep,
                DepthFailOp = StencilOperation.Keep,
                PassOp = StencilOperation.Keep,
                CompareOp = ComparisonOperator.Always,
            },
        };
        var blend = BlendingConfig.CreateNonIndependent(new() {
            EnableBlend = true,

            Source = BlendFactor.SourceAlpha,
            Dest = BlendFactor.InvertSourceAlpha,
            Operator = BlendOperator.Add,
            SourceAlpha = BlendFactor.One,
            DestAlpha = BlendFactor.InvertSourceAlpha,
            AlphaOperator = BlendOperator.Add,
            WriteMask = RenderTargetWriteMask.All,
        });

        var factory = RenderingService.Context.Factory;

        TestPipelineResource = factory.CreateResourceSignature([
            new ResourceTableDescriptor() {
                Table = [
                    new() {
                        Type = ResourceRangeType.ConstantBuffer,
                        BaseRegister = 0,
                        Space = 32,
                        NumResources = 1,
                    },
                ],
            },
        ], []);

        var shader = factory.CreateGraphicalShader(pVSBlob.AsSpan(), pPSBlob.AsSpan(), default, default);

        RenderTargetFormats formats = default;
        formats[0] = GraphicsFormat.R8G8B8A8UNorm;
        TestPipelineState = factory.CreatePipelineState(shader, TestPipelineResource, new() {
            Rasterization = rdesc,
            Blending = blend,
            DepthStencil = depth,
            RenderTargetFormats = new() {
                NumRenderTargets = 1,
                Formats = formats,
            },
            DepthFormat = GraphicsFormat.D24UNormS8UInt,
        });

        shader.DecrementReference();
    }
}