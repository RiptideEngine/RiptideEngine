using Riptide.ShaderCompilation;
using RiptideEngine.Core;
using RiptideFoundation;
using RiptideMathematics;
using RiptideRendering;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;
using Color = RiptideMathematics.Color;

namespace Riptide.RenderingApiExperiment;

internal static unsafe class Lifecycle {
    private static RiptideServices _services = null!;
    private static GpuBuffer _vertexBuffer = null!;
    private static ShaderResourceView _vbview = null!;
    
    private static GpuBuffer _indexBuffer = null!;

    private static GraphicalShader _shader = null!;
    private static ResourceSignature _resourceSignature = null!;
    private static PipelineState _pipelineState = null!;

    private static Texture2D _texture1 = null!, _texture2 = null!;
    
    public static void Initialize() {
        _services = new();
        var renderingCtx = _services.CreateService<IRenderingService, RenderingService, ContextOptions>(new() {
            Api = RenderingAPI.Direct3D12,
            OutputWindow = Runner.MainWindow,
        }).Context;
        _services.CreateService<IInputService, SilkInputService, IView>(Runner.MainWindow);
        
        Graphics.Initialize(_services);
        RuntimeFoundation.Initialize(_services);
        ShaderCompilationPipeline.Initialize(renderingCtx);

        renderingCtx.Logger = _services.CreateService<ILoggingService, LoggingService>();

        _vertexBuffer = renderingCtx.Factory.CreateBuffer(new() {
            Width = (uint)sizeof(Vertex) * 4,
        });
        _vertexBuffer.Name = "Vertex Buffer";
        
        _indexBuffer = renderingCtx.Factory.CreateBuffer(new() {
            Width = sizeof(ushort) * 6,
        });
        _indexBuffer.Name = "Index Buffer";
        
        _vbview = renderingCtx.Factory.CreateShaderResourceView(_vertexBuffer, new() {
            Dimension = ShaderResourceViewDimension.Buffer,
            Buffer = new() {
                FirstElement = 0,
                NumElements = 4,
                StructureSize = (uint)sizeof(Vertex),
            },
        });

        _texture1 = new(32, 32, GraphicsFormat.R8G8B8A8UNorm);
        _texture2 = new(32, 32, GraphicsFormat.R8G8B8A8UNorm);
        
        var cmdList = renderingCtx.Factory.CreateCopyCommandList();
        cmdList.Name = "Copy Command List";
        
        cmdList.UpdateBuffer(_vertexBuffer, MemoryMarshal.AsBytes(stackalloc Vertex[] {
            new(new(-0.5f, 0.5f, 0), Color32.White, Vector2.Zero),
            new(new(0.5f, 0.5f, 0), Color32.White, Vector2.UnitX),
            new(new(0.5f, -0.5f, 0), Color32.White, Vector2.One),
            new(new(-0.5f, -0.5f, 0), Color32.White, Vector2.UnitY),
        }));
        cmdList.UpdateBuffer(_indexBuffer, MemoryMarshal.AsBytes(stackalloc ushort[] {
            0, 1, 2, 2, 3, 0,
        }));

        Color32[] pixels = new Color32[32 * 32];
        for (int y = 0; y < 32; y++) {
            for (int x = 0; x < 32; x++) {
                pixels[y * 32 + x] = new((byte)(x / 31f * 255.9f), 255);
            }
        }

        cmdList.UpdateTexture(_texture1.UnderlyingTexture, 0, MemoryMarshal.AsBytes<Color32>(pixels));
        
        for (int y = 0; y < 32; y++) {
            pixels.AsSpan(y * 32, 32).Fill(new((byte)(y / 31f * 255.9f), 255));
        }

        cmdList.UpdateTexture(_texture2.UnderlyingTexture, 0, MemoryMarshal.AsBytes<Color32>(pixels));
        
        cmdList.Close();
        renderingCtx.ExecuteCommandList(cmdList);
        cmdList.DecrementReference();
        
        renderingCtx.WaitQueueIdle(QueueType.Copy);

        byte[] source = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Shader.hlsl"));

        var compiler = new DxcCompilationPipeline();

        var result = compiler.SetEntrypoint("vsmain").SetTarget("vs_6_0").SetOptimizationLevel(OptimizationLevel.Level3).Compile(source);
        var vsPayload = result.GetPayload(PayloadType.Shader)!;
        result.DecrementReference();

        result = compiler.SetEntrypoint("psmain").SetTarget("ps_6_0").Compile(source);
        var psPayload = result.GetPayload(PayloadType.Shader)!;
        result.DecrementReference();

        _shader = renderingCtx.Factory.CreateGraphicalShader(vsPayload.GetData(), psPayload.GetData(), default, default);
        _resourceSignature = renderingCtx.Factory.CreateResourceSignature(new() {
            Parameters = new[] {
                new ResourceParameter {
                    Type = ResourceParameterType.Table,
                    Table = new() {
                        Ranges = new[] {
                            new ResourceRange {
                                Type = ResourceRangeType.ShaderResourceView,
                                BaseRegister = 0,
                                Space = 0,
                                NumResources = 1,
                            },
                            new ResourceRange {
                                Type = ResourceRangeType.ShaderResourceView,
                                BaseRegister = 0,
                                Space = 1,
                                NumResources = 1,
                            },
                        },
                    },
                },
                new ResourceParameter {
                    Type = ResourceParameterType.Constants,
                    Constants = new() {
                        NumConstants = 16,
                        Register = 0,
                        Space = 0,
                    }
                }
            },
            ImmutableSamplers = new[] {
                new ImmutableSamplerDescription {
                    AddressU = TextureAddressingMode.Clamp,
                    AddressV = TextureAddressingMode.Clamp,
                    AddressW = TextureAddressingMode.Clamp,
                    ComparisonOp = ComparisonOperator.Less,
                    Filter = SamplerFilter.Point,
                    Register = 0,
                    Space = 1,
                },
            },
        });

        RenderTargetFormats formats = default;
        formats[0] = GraphicsFormat.R8G8B8A8UNorm;
        
        _pipelineState = renderingCtx.Factory.CreatePipelineState(_shader, _resourceSignature, new() {
            PrimitiveTopology = PipelinePrimitiveTopology.Triangle,
            Rasterization = RasterizerDescription.Default,
            Blending = BlendingDescription.Disable,
            DepthFormat = GraphicsFormat.Unknown,
            DepthStencil = DepthStencilDescription.Disable,
            RenderTargetFormats = new() {
                NumRenderTargets = 1,
                Formats = formats,
            }
        });
        
        vsPayload.DecrementReference();
        psPayload.DecrementReference();
        
        compiler.Dispose();
    }

    public static void Update(double dt) { }
    public static void Render(double dt) {
        var context = Graphics.RenderingContext;
        
        var cmdList = context.Factory.CreateGraphicsCommandList();
        
        cmdList.TranslateResourceState(context.SwapchainCurrentRenderTarget.Resource, ResourceTranslateStates.RenderTarget);
        
        cmdList.ClearRenderTarget(context.SwapchainCurrentRenderTarget.View, new(0.15f, 1));
        
        cmdList.SetRenderTarget(context.SwapchainCurrentRenderTarget.View, null);
        cmdList.SetViewport(new(0, 0, Runner.MainWindow.Size.X, Runner.MainWindow.Size.Y, 0, 1));
        cmdList.SetScissorRect(new(0, 0, Runner.MainWindow.Size.X, Runner.MainWindow.Size.Y));
        
        cmdList.SetResourceSignature(_resourceSignature);
        cmdList.SetPipelineState(_pipelineState);
        cmdList.SetPrimitiveTopology(RenderingPrimitiveTopology.TriangleList);
        cmdList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16, 0);

        cmdList.SetGraphicsShaderResourceView(0, 0, _vbview);
        
        cmdList.SetGraphicsShaderResourceView(0, 1, _texture1.UnderlyingView);
        
        Matrix4x4 vp = Matrix4x4.CreateLookToLeftHanded(new(0, 0, -3), Vector3.UnitZ, Vector3.UnitY) * Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(70 * float.Pi / 180, (float)Runner.MainWindow.Size.X / Runner.MainWindow.Size.Y, 0.001f, 1000f);
        Matrix4x4 mvp = Matrix4x4.CreateTranslation(-0.5f, 0, 0) * vp;
        cmdList.SetGraphicsConstants(1, new(&mvp, 16), 0);
        
        cmdList.DrawIndexed(6, 1, 0, 0);

        cmdList.SetGraphicsShaderResourceView(0, 1, _texture2.UnderlyingView);
        
        mvp = Matrix4x4.CreateTranslation(0.5f, 0, 0) * vp;
        cmdList.SetGraphicsConstants(1, new(&mvp, 16), 0);
        cmdList.DrawIndexed(6, 1, 0, 0);
        
        cmdList.TranslateResourceState(context.SwapchainCurrentRenderTarget.Resource, ResourceTranslateStates.Present);

        cmdList.Close();
        context.ExecuteCommandList(cmdList);
        cmdList.DecrementReference();

        context.Present();
    }

    public static void Shutdown() {
        _texture1?.DecrementReference();
        _texture2?.DecrementReference();
        
        _resourceSignature?.DecrementReference();
        _pipelineState?.DecrementReference();
        _shader?.DecrementReference();
        
        _vbview.DecrementReference();
        _vertexBuffer.DecrementReference();
        _indexBuffer.DecrementReference();
        
        ShaderCompilationPipeline.Shutdown();
        RuntimeFoundation.Shutdown();

        _services.RemoveAllServices();
    }

    private readonly record struct Vertex(Vector3 Position, Color32 Color, Vector2 TexCoord);
}