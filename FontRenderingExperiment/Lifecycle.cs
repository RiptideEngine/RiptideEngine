using Riptide.LowLevel.TextEngine;
using Riptide.ShaderCompilation;
using RiptideEngine.Core;
using RiptideFoundation;
using RiptideMathematics;
using RiptideRendering;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DxcBuffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace Riptide.FontRenderingExperiment;

internal static unsafe class Lifecycle {
    private static RiptideServices _services = null!;
    public static RenderingContext Context { get; private set; } = null!;

    //private static GpuTexture _depthTexture = null!;
    //private static DepthStencilView _depthStencilView = null!;

    private static Mesh _quad = null!;
    private static Mesh _textMesh = null!;
    private static GpuBuffer _constantBuffer = null!;

    private static GraphicalShader _shader = null!, _wireframeShader = null!;
    private static PipelineState _pipelineState = null!, _wireframePipelineState = null!;
    private static ResourceSignature _resourceSignature = null!;

    private static Font _font = null!;

    private const string Text = "Line 1\nLine 2\nLine 3";

    public static void Initialize() {
        _services = new();
        Context = _services.CreateService<IRenderingService, RenderingService, ContextOptions>(new() {
            Api = RenderingAPI.Direct3D12,
            OutputWindow = Runner.MainWindow,
        }).Context;
        _services.CreateService<IInputService, SilkInputService, IView>(Runner.MainWindow);
        
        Graphics.Initialize(_services);
        RuntimeFoundation.Initialize(_services);
        ShaderCompilationPipeline.Initialize(Context);
        FontEngine.Initialize();

        Context.Logger = _services.CreateService<ILoggingService, LoggingService>();

        var factory = Context.Factory;

        _quad = new() {
            Name = "Quad",
        };
        _quad.AllocateVertexBuffers(4, stackalloc VertexDescriptor[] {
            new((uint)sizeof(Vertex), 0),
        });
        _quad.AllocateIndexBuffer(6, IndexFormat.UInt16);

        _textMesh = new() {
            Name = "Text Mesh",
        };
        _textMesh.AllocateVertexBuffers((uint)Text.Length * 4U, stackalloc VertexDescriptor[] {
            new((uint)sizeof(Vertex), 0),
        });
        _textMesh.AllocateIndexBuffer((uint)Text.Length * 6, IndexFormat.UInt16);

        _constantBuffer = factory.CreateBuffer(new() {
            Width = 256,
        });
        _constantBuffer.Name = "Constant Buffer";

        _font = Font.Import(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "times.ttf"), new(1024, 1024), 128, stackalloc CodepointRange[] {
            new(32, 127),
        })!;
        _font.Name = "Font";

        // _depthTexture = factory.CreateTexture(new() {
        //     Dimension = TextureDimension.Texture2D,
        //     Width = (uint)Runner.MainWindow.Size.X,
        //     Height = (ushort)Runner.MainWindow.Size.Y,
        //     DepthOrArraySize = 1,
        //     Flags = TextureFlags.DepthStencil,
        //     Format = GraphicsFormat.D24UNormS8UInt,
        // });
        // _depthTexture.Name = "Depth Texture";
        //
        // _depthStencilView = factory.CreateDepthStencilView(_depthTexture, new() {
        //     Dimension = DepthStencilViewDimension.Texture2D,
        //     Format = GraphicsFormat.D24UNormS8UInt,
        //     Texture2D = new(),
        // });

        var cmdList = factory.CreateCommandList();

        cmdList.TranslateState(_textMesh.GetVertexBuffer(0).Buffer, ResourceTranslateStates.CopyDestination);
        cmdList.TranslateState(_textMesh.IndexBuffer!, ResourceTranslateStates.CopyDestination);
        
        cmdList.TranslateState(_quad.GetVertexBuffer(0).Buffer, ResourceTranslateStates.CopyDestination);
        cmdList.TranslateState(_quad.IndexBuffer!, ResourceTranslateStates.CopyDestination);
        
        cmdList.TranslateState(_constantBuffer, ResourceTranslateStates.ConstantBuffer);
        //cmdList.TranslateState(_depthTexture, ResourceTranslateStates.DepthRead);
        
        cmdList.UpdateResource(_quad.GetVertexBuffer(0).Buffer, (span, _, _) => {
            var vertices = MemoryMarshal.Cast<byte, Vertex>(span);

            vertices[0] = new(new(0f, 0f), Vector2.Zero, Color32.White);
            vertices[1] = new(new(512f, 0), Vector2.UnitX, Color32.White);
            vertices[2] = new(new(512f, 512f), Vector2.One, Color32.White);
            vertices[3] = new(new(0f, 512f), Vector2.UnitY, Color32.White);
        }, 0);
        cmdList.UpdateResource(_quad.IndexBuffer!, MemoryMarshal.Cast<ushort, byte>(stackalloc ushort[] { 0, 1, 2, 2, 3, 0, }));
        cmdList.UpdateResource(_textMesh.GetVertexBuffer(0).Buffer, static (span, _, scale) => {
            var vertices = MemoryMarshal.Cast<byte, Vertex>(span);

            Vector2 position = new(600, 400);
            Font.CodepointInformation info;
            
            int vertexIndex = 0;
            for (int i = 0, len = Text.Length; i < len;) {
                switch (Text[i]) {
                    case '\n':
                        position = new(600, position.Y + _font.Height * scale);
                        i++;
                        break;
                    
                    case '\r':
                        i++; // Ignore.
                        break;
                    
                    case '\t':
                        i++;
                        if (!_font.TryGetCodepointInformation(32, out info)) break;

                        position.X += info.Metric.Size.X * scale;
                        break;
                    
                    default:
                        int codepoint = char.ConvertToUtf32(Text, i);
                        
                        if (char.IsSurrogatePair(Text, i)) {
                            i += 2;
                        } else {
                            i += 1;
                        }

                        if (!_font.TryGetCodepointInformation(codepoint, out info)) break;
                        
                        var metric = info.Metric;
                        var bb = info.TextureBoundary;
                        
                        Vector2 topLeft = position + new Vector2(metric.Bearing.X, -metric.Bearing.Y) * scale;
                        Vector2 bottomRight = topLeft + new Vector2(metric.Size.X, metric.Size.Y) * scale;
                        
                        vertices[vertexIndex + 0] = new(topLeft, bb.Min, Color32.White);
                        vertices[vertexIndex + 1] = new(topLeft with { X = bottomRight.X }, bb.Min with { X = bb.Max.X }, Color32.White);
                        vertices[vertexIndex + 2] = new(bottomRight, bb.Max, Color32.White);
                        vertices[vertexIndex + 3] = new(bottomRight with { X = topLeft.X }, bb.Min with { Y = bb.Max.Y }, Color32.White);
                        
                        vertexIndex += 4;
                        position.X += info.Metric.Advance.X * scale;
                        break;
                }
            }
            
            //for (int i = 0; i < Text.Length; i += char.IsSurrogatePair(Text, i) ? 2 : 1) {
            //     var codepoint = char.ConvertToUtf32(Text, i);
            //     
            //     if (!_font.TryGetCodepointInformation(codepoint, out var info)) continue;
            //
            //     var metric = info.Metric;
            //     var bb = info.TextureBoundary;
            //
            //     Vector2 topLeft = cursorPosition + new Vector2(metric.Bearing.X, -metric.Bearing.Y) * scale;
            //     Vector2 bottomRight = topLeft + new Vector2(metric.Size.X, metric.Size.Y) * scale;
            //
            //     vertices[vindex + 0] = new(topLeft, bb.Min, Color32.White);
            //     vertices[vindex + 1] = new(topLeft with { X = bottomRight.X }, bb.Min with { X = bb.Max.X }, Color32.White);
            //     vertices[vindex + 2] = new(bottomRight, bb.Max, Color32.White);
            //     vertices[vindex + 3] = new(bottomRight with { X = topLeft.X }, bb.Min with { Y = bb.Max.Y }, Color32.White);
            //     
            //     vindex += 4;
            //     cursorPosition.X += info.Metric.Advance.X * scale;
            //}
        }, 256f / _font.Size);
        cmdList.UpdateResource(_textMesh.IndexBuffer!, (span, _, _) => {
            var indices = MemoryMarshal.Cast<byte, ushort>(span);
            
            int vindex = 0;
            int iindex = 0;
            for (int i = 0; i < Text.Length; i += char.IsSurrogatePair(Text, i) ? 2 : 1) {
                var codepoint = char.ConvertToUtf32(Text, i);
            
                if (!_font.ContainsCodepoint(codepoint)) continue;
                 
                indices[iindex] = (ushort)vindex;
                indices[iindex + 1] = (ushort)(vindex + 1);
                indices[iindex + 2] = (ushort)(vindex + 2);
                indices[iindex + 3] = (ushort)(vindex + 2);
                indices[iindex + 4] = (ushort)(vindex + 3);
                indices[iindex + 5] = (ushort)vindex;
                
                vindex += 4;
                iindex += 6;
            }
        }, 0);
        
        cmdList.TranslateState(_textMesh.GetVertexBuffer(0).Buffer, ResourceTranslateStates.ShaderResource);
        cmdList.TranslateState(_textMesh.IndexBuffer!, ResourceTranslateStates.IndexBuffer);
        
        cmdList.TranslateState(_quad.GetVertexBuffer(0).Buffer, ResourceTranslateStates.ShaderResource);
        cmdList.TranslateState(_quad.IndexBuffer!, ResourceTranslateStates.IndexBuffer);

        cmdList.Close();
        Context.ExecuteCommandList(cmdList);
        cmdList.DecrementReference();

        ReadOnlySpan<byte> source = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Shader.hlsl"));

        var compiler = ShaderCompilationPipeline.Pipeline.ResetDefault();
        
        var vsResult = compiler.SetOptimizationLevel(OptimizationLevel.Level3)
                               .SetEntrypoint("vsmain")
                               .SetTarget("vs_6_0")
                               .Compile(source);
        
        Debug.Assert(vsResult.Status, "vsResult.Status");

        var psResult = compiler.SetEntrypoint("psmain").SetTarget("ps_6_0").Compile(source);
        
        Debug.Assert(psResult.Status, "psResult.Status");

        CompiledPayload vsPayload = vsResult.GetPayload(PayloadType.Shader)!;
        CompiledPayload psPayload = psResult.GetPayload(PayloadType.Shader)!;
        
        _shader = factory.CreateGraphicalShader(vsPayload.GetData(), psPayload.GetData(), default, default);

        vsPayload.DecrementReference();
        psPayload.DecrementReference();
        
        vsResult.DecrementReference();
        psResult.DecrementReference();
        compiler.Dispose();
        
        // using ComPtr<IDxcBlob> pVsBytecode = default;
        // using ComPtr<IDxcBlob> pPsBytecode = default;
        //
        // ShaderCompileUtils.CompileShader(source, "vs_6_0", "vsmain", "-O3", default, pVsBytecode.GetAddressOf());
        // ShaderCompileUtils.CompileShader(source, "ps_6_0", "psmain", "-O3", default, pPsBytecode.GetAddressOf());
        // _shader = factory.CreateGraphicalShader(pVsBytecode.AsSpan(), pPsBytecode.AsSpan(), default, default);
        //
        // source = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Wireframe.hlsl"));
        //
        // using ComPtr<IDxcBlob> pVsBytecode2 = default;
        // using ComPtr<IDxcBlob> pPsBytecode2 = default;
        //
        // ShaderCompileUtils.CompileShader(source, "vs_6_0", "vsmain", "-O3", default, pVsBytecode2.GetAddressOf());
        // ShaderCompileUtils.CompileShader(source, "ps_6_0", "psmain", "-O3", default, pPsBytecode2.GetAddressOf());
        // _wireframeShader = factory.CreateGraphicalShader(pVsBytecode2.AsSpan(), pPsBytecode2.AsSpan(), default, default);

        _resourceSignature = factory.CreateResourceSignature(new() {
            Parameters = new ResourceParameter[] {
                new() {
                    Type = ResourceParameterType.Table,
                    Table = new() {
                        Ranges = new ResourceRange[] {
                            new() {
                                Type = ResourceRangeType.ShaderResourceView,
                                BaseRegister = 0,
                                Space = 0,
                                NumResources = 1,
                            },
                            new() {
                                Type = ResourceRangeType.ConstantBuffer,
                                NumResources = 1,
                                BaseRegister = 0,
                                Space = 0,
                            },
                            new() {
                                Type = ResourceRangeType.ShaderResourceView,
                                NumResources = 1,
                                BaseRegister = 0,
                                Space = 1,
                            },
                        },
                    },
                },
            },
            ImmutableSamplers = new[] {
                new ImmutableSamplerDescription {
                    AddressU = TextureAddressingMode.Clamp,
                    AddressV = TextureAddressingMode.Clamp,
                    AddressW = TextureAddressingMode.Clamp,
                    Filter = SamplerFilter.Linear,
                    ComparisonOp = ComparisonOperator.Less,
                    Register = 0,
                    Space = 1,
                },
            },
        });

        RenderTargetFormatDescription formats = new() {
            NumRenderTargets = 1,
        };
        formats.Formats[0] = GraphicsFormat.R8G8B8A8UNorm;

        var psodesc = new PipelineStateDescription() {
            Rasterization = new() {
                CullMode = CullingMode.None,
                FillMode = FillingMode.Solid,
            },
            Blending = BlendingDescription.Transparent,
            DepthStencil = new() {
                EnableDepth = false,
                EnableStencil = false,
                DepthComparison = ComparisonOperator.Always,
                BackfaceOperation = new() {
                    DepthFailOp = StencilOperation.Keep,
                    FailOp = StencilOperation.Keep,
                    PassOp = StencilOperation.Keep,
                    CompareOp = ComparisonOperator.Always,
                },
                FrontFaceOperation = new() {
                    DepthFailOp = StencilOperation.Keep,
                    FailOp = StencilOperation.Keep,
                    PassOp = StencilOperation.Keep,
                    CompareOp = ComparisonOperator.Always,
                },
            },
            RenderTargetFormats = formats,
            DepthFormat = GraphicsFormat.Unknown,
            PrimitiveTopology = PipelinePrimitiveTopology.Triangle,
        };
        _pipelineState = factory.CreatePipelineState(_shader, _resourceSignature, psodesc);
        _pipelineState.Name = "Pipeline State";

        // _wireframePipelineState = factory.CreatePipelineState(_wireframeShader, _resourceSignature, psodesc with {
        //     Rasterization = new() {
        //         Conservative = false,
        //         CullMode = CullingMode.None,
        //         FillMode = FillingMode.Wireframe,
        //     },
        // });
        // _wireframePipelineState.Name = "Wireframe Pipeline State";
    }

    public static void Update(double dt) {
    }

    public static void Render(double dt) {
        Matrix4x4 projectionMatrix = new(2f / Runner.MainWindow.Size.X, 0, 0, 0, 0, -2f / Runner.MainWindow.Size.Y, 0, 0, 0, 0, 0.5f, 0, -1f, 1f, 0.5f, 1f);
        
        var cmdList = Context.Factory.CreateCommandList();
        
        cmdList.TranslateState(Context.SwapchainCurrentRenderTarget.Resource, ResourceTranslateStates.RenderTarget);
        //cmdList.TranslateState(_depthTexture, ResourceTranslateStates.DepthWrite);

        cmdList.SetRenderTarget(Context.SwapchainCurrentRenderTarget.View, null);
        cmdList.ClearRenderTarget(Context.SwapchainCurrentRenderTarget.View, new(0, 0.2f, 0.4f, 1), []);
        //cmdList.ClearDepthTexture(_depthStencilView, DepthClearFlags.All, 1f, 0, []);
        cmdList.SetScissorRect(new(0, 0, Runner.MainWindow.Size.X, Runner.MainWindow.Size.Y));
        cmdList.SetViewport(new(0, 0, Runner.MainWindow.Size.X, Runner.MainWindow.Size.Y));

        cmdList.SetGraphicsResourceSignature(_resourceSignature);
        cmdList.SetPipelineState(_pipelineState);
        cmdList.SetPrimitiveTopology(RenderingPrimitiveTopology.TriangleList);
        cmdList.SetGraphicsConstantBuffer(0, 1, _constantBuffer, 0);
        
        cmdList.TranslateState(_constantBuffer, ResourceTranslateStates.CopyDestination);
        cmdList.UpdateResource(_constantBuffer, (span, _, matrix) => {
            fixed (byte* pDestination = span) {
                Unsafe.Write(pDestination, matrix);
            }
        }, projectionMatrix);
        cmdList.TranslateState(_constantBuffer, ResourceTranslateStates.ConstantBuffer);

        cmdList.SetGraphicsShaderResourceView(0, 0, _quad.GetVertexBuffer(0).View);
        cmdList.SetIndexBuffer(_quad.IndexBuffer!, IndexFormat.UInt16, 0);
        cmdList.SetGraphicsShaderResourceView(0, 2, _font.Bitmap.UnderlyingView);
        cmdList.DrawIndexed(_quad.IndexCount, 1, 0, 0);

        cmdList.TranslateState(_constantBuffer, ResourceTranslateStates.CopyDestination);
        cmdList.UpdateResource(_constantBuffer, (span, _, matrix) => {
            fixed (byte* pDestination = span) {
                Unsafe.Write(pDestination, Matrix4x4.CreateTranslation(0.6f, -0.5f, 0) * matrix);
            }
        }, projectionMatrix);
        cmdList.TranslateState(_constantBuffer, ResourceTranslateStates.ConstantBuffer);
        
        cmdList.SetGraphicsShaderResourceView(0, 0, _textMesh.GetVertexBuffer(0).View);
        cmdList.SetIndexBuffer(_textMesh.IndexBuffer!, IndexFormat.UInt16, 0);
        cmdList.SetGraphicsShaderResourceView(0, 2, _font.Bitmap.UnderlyingView);
        cmdList.DrawIndexed(_textMesh.IndexCount, 1, 0, 0);
        
        //cmdList.SetPipelineState(_wireframePipelineState);
        //cmdList.DrawIndexed(_textMesh.IndexCount, 1, 0, 0);
        
        cmdList.TranslateState(Context.SwapchainCurrentRenderTarget.Resource, ResourceTranslateStates.Present);
        //cmdList.TranslateState(_depthTexture, ResourceTranslateStates.DepthRead);

        cmdList.Close();
        Context.ExecuteCommandList(cmdList);
        cmdList.DecrementReference();

        Context.Present();
    }

    public static void Shutdown() {
        //_depthTexture.DecrementReference();
        //_depthStencilView.DecrementReference();

        _resourceSignature.DecrementReference();
        _shader.DecrementReference();
        _pipelineState.DecrementReference();
        
        //_wireframeShader.DecrementReference();
        //_wireframePipelineState.DecrementReference();

        _constantBuffer.DecrementReference();

        _font.DecrementReference();

        _textMesh.DecrementReference();
        _quad.DecrementReference();

        FontEngine.Shutdown();
        ShaderCompilationPipeline.Shutdown();
        RuntimeFoundation.Shutdown();

        _services.RemoveAllServices();
    }

    private readonly record struct Vertex(Vector2 Position, Vector2 TexCoord, Color32 Color);
}