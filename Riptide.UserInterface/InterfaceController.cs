using Riptide.ShaderCompilation;
using RiptideRendering;
using System.Reflection;
using System.Resources;

namespace Riptide.UserInterface;

public sealed unsafe class InterfaceController : IDisposable {
    private static GraphicalShader _shader = null!;
    private static ResourceSignature _resourceSig = null!;
    private static PipelineState _defaultPipelineState = null!;

    private readonly Queue<RenderingList> _pooledRenderingList;
    private readonly List<RenderingList> _renderingLists;

    private GpuBuffer _vertexBuffer;
    private ShaderResourceView _vbview;

    private GpuBuffer _indexBuffer;
    
    public Rectangle2D Viewport { get; set; }

    private uint _vertexBufferCount = 1024;
    private uint _indexBufferCount = 2048;

    private readonly BaseRenderingContext _renderingContext;

    private Texture2D _emptyTexture;
    
    public InterfaceController(BaseRenderingContext context) {
        var factory = context.Factory;
        
        if (_shader == null) {
            using (var shaderStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Riptide.UserInterface.UI-Primitive.hlsl") ?? throw new MissingManifestResourceException("Cannot found manifest shader resource.")) {
                byte[] source = new byte[shaderStream.Length];
                shaderStream.ReadExactly(source);

                var result = ShaderCompilationPipeline.Pipeline.ResetDefault()
                                                               .SetEntrypoint("vsmain")
                                                               .SetTarget("vs_6_0")
                                                               .SetOptimizationLevel(OptimizationLevel.Level3)
                                                               .Compile(source);
                
                Debug.Assert(result.Status, "result.Status");

                var vsPayload = result.GetPayload(PayloadType.Shader);

                result.DecrementReference();

                result = ShaderCompilationPipeline.Pipeline.ResetDefault()
                                                           .SetEntrypoint("psmain")
                                                           .SetTarget("ps_6_0")
                                                           .SetOptimizationLevel(OptimizationLevel.Level3)
                                                           .Compile(source);

                Debug.Assert(result.Status, "result.Status");

                var psPayload = result.GetPayload(PayloadType.Shader);
                
                result.DecrementReference();
                
                _shader = factory.CreateGraphicalShader(vsPayload!.GetData(), psPayload!.GetData(), default, default);

                vsPayload.DecrementReference();
                psPayload.DecrementReference();
            }

            _resourceSig = factory.CreateResourceSignature(new() {
                ImmutableSamplers = [
                    new() {
                        AddressU = TextureAddressingMode.Wrap,
                        AddressV = TextureAddressingMode.Wrap,
                        AddressW = TextureAddressingMode.Wrap,
                        Filter = SamplerFilter.Linear,
                        MinLod = 0, MaxLod = 0, MipLodBias = 0,
                        ComparisonOp = ComparisonOperator.Always,
                        MaxAnisotropy = 0,
                        Register = 0,
                        Space = 0,
                    },
                ],
                Parameters = [
                    new() {
                        Type = ResourceParameterType.Constants,
                        Constants = new() {
                            NumConstants = 17,
                            Register = 0,
                            Space = 0,
                        },
                    },
                    new() {
                        Type = ResourceParameterType.Table,
                        Table = new() {
                            Ranges = [
                                new() {
                                    BaseRegister = 0,
                                    Space = 0,
                                    NumResources = 1,
                                    Type = ResourceRangeType.ShaderResourceView,
                                },
                            ],
                        },
                    },
                    new() {
                        Type = ResourceParameterType.Table,
                        Table = new() {
                            Ranges = [
                                new() {
                                    BaseRegister = 1,
                                    Space = 0,
                                    NumResources = 1,
                                    Type = ResourceRangeType.ShaderResourceView,
                                },
                            ],
                        },
                    },
                ],
                Flags = SignatureFlags.None,
            });
            
            RenderTargetFormatDescription formats = new() {
                NumRenderTargets = 1,
            };
            formats.Formats[0] = GraphicsFormat.R8G8B8A8UNorm;

            PipelineStateDescription psodesc = new() {
                Rasterization = new() {
                    CullMode = CullingMode.None,
                    FillMode = FillingMode.Solid,
                },
                Blending = BlendingDescription.Transparent,
                DepthStencil = DepthStencilDescription.Disable,
                RenderTargetFormats = formats,
                DepthFormat = GraphicsFormat.Unknown,
                PrimitiveTopology = PipelinePrimitiveTopology.Triangle,
            };
            
            _defaultPipelineState = factory.CreatePipelineState(_shader, _resourceSig, psodesc);
        }

        _vertexBuffer = factory.CreateBuffer(new() {
            Width = (ulong)sizeof(Vertex) * _vertexBufferCount,
        });
        _vertexBuffer.Name = "InterfaceController.VertexBuffer";
        
        _indexBuffer = factory.CreateBuffer(new() {
            Width = (ulong)sizeof(ushort) * _indexBufferCount,
        });
        _indexBuffer.Name = "InterfaceController.IndexBuffer";
        
        _vbview = factory.CreateShaderResourceView(_vertexBuffer, new() {
            Dimension = ShaderResourceViewDimension.Buffer,
            Buffer = new() {
                FirstElement = 0,
                NumElements = _vertexBufferCount,
                StructureSize = (uint)sizeof(Vertex),
                IsRaw = false,
            },
        });
        _vbview.Name = "InterfaceController.VertexBufferView";

        _emptyTexture = new(4, 4, GraphicsFormat.R8G8B8A8UNorm) {
            Name = "InterfaceController.EmptyTexture",
        };

        var cmdList = context.Factory.CreateCommandList();
        cmdList.TranslateState(_emptyTexture.UnderlyingTexture, ResourceTranslateStates.CopyDestination);

        Span<byte> pixels = stackalloc byte[sizeof(Color32) * 16];
        pixels.Fill(255);
        cmdList.UpdateResource(_emptyTexture.UnderlyingTexture, pixels);
        cmdList.TranslateState(_emptyTexture.UnderlyingTexture, ResourceTranslateStates.ShaderResource);
        
        cmdList.Close();
        context.ExecuteCommandList(cmdList);
        context.WaitForGpuIdle();
        cmdList.DecrementReference();
        
        _pooledRenderingList = [];
        _renderingLists = [];

        _renderingContext = context;
    }
    
    public void Update() {
        
    }

    public void Render(CommandList cmdList) {
        int totalVertexCount = 0, totalIndexCount = 0;
        foreach (var list in _renderingLists) {
            totalVertexCount += list.VertexCount;
            totalIndexCount += list.IndexCount;
        }

        if (totalVertexCount > _vertexBufferCount) {
            _vertexBuffer.DecrementReference();
            _vbview.DecrementReference();

            _vertexBuffer = _renderingContext.Factory.CreateBuffer(new() {
                Width = (uint)sizeof(Vertex) * (uint)totalVertexCount,
            });
            _vbview = _renderingContext.Factory.CreateShaderResourceView(_vertexBuffer, new() {
                Dimension = ShaderResourceViewDimension.Buffer,
                Buffer = new() {
                    FirstElement = 0,
                    NumElements = (uint)totalVertexCount,
                    StructureSize = (uint)sizeof(Vertex),
                    IsRaw = false,
                },
            });

            _vertexBufferCount = (uint)totalVertexCount;
        }
        if (totalIndexCount > _indexBufferCount) {
            _indexBuffer.DecrementReference();

            _indexBuffer = _renderingContext.Factory.CreateBuffer(new() {
                Width = sizeof(ushort) * (uint)totalIndexCount,
            });
            
            _indexBufferCount = (uint)totalIndexCount;
        }
        
        cmdList.TranslateState(_vertexBuffer, ResourceTranslateStates.CopyDestination);
        cmdList.TranslateState(_indexBuffer, ResourceTranslateStates.CopyDestination);
        
        uint vertexOffset = 0, indexOffset = 0;
        foreach (var list in _renderingLists) {
            cmdList.UpdateBufferRegion(_vertexBuffer, vertexOffset * (uint)sizeof(Vertex), MemoryMarshal.AsBytes(list.AllocatedVertices));
            cmdList.UpdateBufferRegion(_indexBuffer, indexOffset * sizeof(ushort), MemoryMarshal.AsBytes(list.AllocatedIndices));

            vertexOffset += (uint)list.VertexCount;
            indexOffset += (uint)list.IndexCount;
        }
        
        cmdList.TranslateState(_vertexBuffer, ResourceTranslateStates.ShaderResource);
        cmdList.TranslateState(_indexBuffer, ResourceTranslateStates.IndexBuffer);
        
        cmdList.SetPipelineState(_defaultPipelineState);
        cmdList.SetGraphicsResourceSignature(_resourceSig);
        
        cmdList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16, 0);
        cmdList.SetGraphicsShaderResourceView(1, 0, _vbview);
        cmdList.SetPrimitiveTopology(RenderingPrimitiveTopology.TriangleList);

        {
            Matrix4x4 projection = new(2f / Viewport.Size.X, 0, 0, 0, 0, -2f / Viewport.Size.Y, 0, 0, 0, 0, 0.5f, 0, (Viewport.Position.X * 2 + Viewport.Size.X) / -Viewport.Size.X, (Viewport.Position.Y * 2 + Viewport.Size.Y) / Viewport.Size.Y, 0.5f, 1f);
            cmdList.SetGraphicsConstants(0, new(&projection, 16), 0);
        }
        
        cmdList.SetGraphicsConstants(0, [ 4 ], 16);

        vertexOffset = indexOffset = 0;
        
        foreach (var list in _renderingLists) {
            foreach (ref readonly var cmd in list.RenderingCommands) {
                if (cmd.IndexCount == 0) continue;

                cmdList.SetScissorRect(cmd.ScissorRect);
                cmdList.SetGraphicsShaderResourceView(2, 0, cmd.TextureView ?? _emptyTexture.UnderlyingView);
        
                uint offset = vertexOffset + cmd.VertexOffset;
                cmdList.SetGraphicsConstants(0, MemoryMarshal.CreateSpan(ref offset, 1), 16);
                
                cmdList.DrawIndexed(cmd.IndexCount, 1, indexOffset + cmd.IndexOffset, 0);
            }

            vertexOffset += (uint)list.VertexCount;
            indexOffset += (uint)list.IndexCount;
        }
        
        _renderingLists.Clear();
    }

    public RenderingList GetRenderingList() {
        if (_pooledRenderingList.TryDequeue(out var dequeue)) {
            dequeue.Reset();
            return dequeue;
        }

        return new();
    }

    public void EnqueueRenderingList(RenderingList list) {
        _pooledRenderingList.Enqueue(list);
        _renderingLists.Add(list);
    }

    public void Dispose() {
        _vertexBuffer.DecrementReference();
        _vbview.DecrementReference();
        _indexBuffer.DecrementReference();

        _emptyTexture.DecrementReference();
        _emptyTexture = null!;
        
        _shader.DecrementReference();
        _resourceSig.DecrementReference();
        _defaultPipelineState.DecrementReference();
    }
}