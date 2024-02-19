using Riptide.ShaderCompilation;
using RiptideFoundation.Rendering;
using System.Reflection;

namespace RiptideEditorV2.UI;

public sealed unsafe partial class InterfaceRenderer : IDisposable {
    private readonly Dictionary<MaterialPipeline, Dictionary<int, BatchedCandidate>> _batchedCandidates = [];
    private readonly HashSet<VisualElement> _dirtyElements = [];

    private readonly InterfaceDocument _document;
    
    internal MaterialPipeline DefaultMaterialPipeline { get; }

    private bool _disposed;

    internal InterfaceRenderer(InterfaceDocument document, RenderingContext context) {
        var factory = context.Factory;

        GraphicalShader shader;
        CompactedShaderReflection reflection;

        var compiler = Graphics.CompilationFactory.CreateCompilationPipeline();

        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RiptideEditorV2.Resources.Shaders.UI-Normal.hlsl")!) {
            byte[] source = new byte[stream.Length];
            stream.ReadExactly(source);

            var vsResult = compiler.SetEntrypoint("vsmain")
                                   .SetOptimizationLevel(OptimizationLevel.Level3)
                                   .SetTarget("vs_6_0")
                                   .Compile(source);
            var psResult = compiler.SetEntrypoint("psmain")
                                   .SetTarget("ps_6_0")
                                   .Compile(source);
            
            shader = factory.CreateGraphicalShader(vsResult.GetShaderBytecode().GetData(), psResult.GetShaderBytecode().GetData());
            
            var vsReflection = ShaderCompilationEngine.CreateReflection(vsResult.GetReflectionInfo());
            var psReflection = ShaderCompilationEngine.CreateReflection(psResult.GetReflectionInfo());

            reflection = new(vsReflection, psReflection);
            
            vsResult.DecrementReference();
            psResult.DecrementReference();

            vsReflection.DecrementReference();
            psReflection.DecrementReference();
        }
        
        RenderTargetFormats formats = default;
        formats[0] = GraphicsFormat.R8G8B8A8UNorm;

        DefaultMaterialPipeline = new(shader, reflection, new() {
            Parameters = [
                ResourceParameter.CreateDescriptors(DescriptorTableType.ShaderResourceView, 1, 0, 0),
                ResourceParameter.CreateConstants(16, 0, 0),
                ResourceParameter.CreateDescriptors(DescriptorTableType.ShaderResourceView, 2, 0, 1),
            ],
            ImmutableSamplers = [
                new() {
                    AddressU = TextureAddressingMode.Clamp,
                    AddressV = TextureAddressingMode.Clamp,
                    AddressW = TextureAddressingMode.Clamp,
                    ComparisonOp = ComparisonOperator.Never,
                    Filter = SamplerFilter.Point,
                    Register = 0,
                    Space = 1,
                    MaxAnisotropy = 0,
                },
            ],
        }, new() {
            Rasterization = new() {
                Conservative = false,
                CullMode = CullingMode.None,
                FillMode = FillingMode.Solid,
            },
            Blending = BlendingDescription.Transparent,
            PrimitiveTopology = PipelinePrimitiveTopology.Triangle,
            RenderTargetFormats = new() {
                NumRenderTargets = 1,
                Formats = formats,
            },
            DepthFormat = GraphicsFormat.Unknown,
            DepthStencil = DepthStencilDescription.Disable,
        }) {
            Name = "InterfaceDocument's Default Material",
        };

        shader.DecrementReference();

        _document = document;
    }

    public void MarkElementDirty(VisualElement element) {
        _dirtyElements.Add(element);
    }

    internal void RebuildDirtiedElements() {
        if (_dirtyElements.Count == 0) return;
        
        var renderingContext = Graphics.RenderingContext;
        var cmdList = renderingContext.Factory.CreateCommandList();
        
        var batching = new Dictionary<int, BatchingCandidate>();
        
        foreach (var group in _dirtyElements.GroupBy(x => x.Pipeline)) {
            var pipeline = group.Key;
            
            Debug.Assert(pipeline != null, "pipeline != null");
            
            if (_batchedCandidates.TryGetValue(pipeline, out var batched)) {
                GetPipelineBatcher(pipeline).BuildBatch(pipeline, _document.Root, Matrix3x2.Identity, new(batching, batched!, pipeline));
                
                foreach ((var hash, var candidate) in batching) {
                    var builder = candidate.Builder;
                
                    if (builder.WrittenIndexByteLength == 0) {
                        batched.Remove(hash, out var batchedCandidateRemoval);
                        
                        batchedCandidateRemoval.Mesh?.DecrementReference();
                        batchedCandidateRemoval.Properties?.DecrementReference();
                    } else {
                        if (batched.TryGetValue(hash, out var existedCandidate)) {
                            builder.Commit(cmdList, existedCandidate.Mesh, false);
                            existedCandidate.Mesh.SetSubmesh(0, new(default, 0, (uint)builder.WrittenIndexByteLength, IndexFormat.UInt16));
                        } else {
                            var mesh = builder.Commit(cmdList);
                            mesh.SetSubmeshes([new(default, 0, mesh.IndexSize, IndexFormat.UInt16)]);
                            
                            batched.Add(hash, new(mesh, candidate.Properties));
                        }
                    }
                
                    builder.Dispose();
                }
            } else {
                batched = new();
                
                GetPipelineBatcher(pipeline).BuildBatch(pipeline, _document.Root, Matrix3x2.Identity, new(batching, batched!, pipeline));
                
                foreach ((var hash, var candidate) in batching) {
                    if (candidate.Builder.WrittenIndexByteLength == 0) {
                        candidate.Properties.DecrementReference();
                        candidate.Builder.Dispose();
                        continue;
                    }
                    
                    var mesh = candidate.Builder.Commit(cmdList);
                    mesh.SetSubmeshes([new(default, 0, mesh.IndexSize, IndexFormat.UInt16)]);
                    
                    batched.Add(hash, new(mesh, candidate.Properties));
                    
                    candidate.Builder.Dispose();
                }
                
                _batchedCandidates.Add(pipeline, batched);
            }
            
            batching.Clear();
        }
        
        cmdList.Close();
        renderingContext.ExecuteCommandList(cmdList);
        cmdList.DecrementReference();

        _dirtyElements.Clear();
    }

    internal void Render(CommandList cmdList, Vector2 displaySize) {
        cmdList.SetPrimitiveTopology(RenderingPrimitiveTopology.TriangleList);
        Matrix4x4 projection = new Matrix4x4(2f / displaySize.X, 0, 0, 0, 0, -2f / displaySize.Y, 0, 0, 0, 0, 0.5f, 0, -1, 1, 0.5f, 1);
        
        foreach ((var pipeline, var batch) in _batchedCandidates) {
            if (batch.Count == 0) continue;
            
            pipeline.BindGraphics(cmdList);
            
            foreach ((_, var batched) in batch) {
                var mesh = batched.Mesh;
                var properties = batched.Properties;
                
                Debug.Assert(mesh != null && mesh.GetReferenceCount() != 0, "mesh != null && mesh.GetReferenceCount() != 0");
                Debug.Assert(properties != null && properties.GetReferenceCount() != 0, "properties != null && properties.GetReferenceCount() != 0");
                
                cmdList.SetIndexBuffer(mesh.IndexBuffer!, IndexFormat.UInt16, 0);

                properties.SetConstants("_Transformation", new(&projection, 16));
                properties.SetBuffer("_Vertices", mesh.GetVertexBuffer(0));

                properties.BindGraphics(cmdList);

                cmdList.DrawIndexed(mesh.Submeshes[0].IndexCount, 1, 0, 0);
            }
        }
    }

    private void Dispose(bool disposing) {
        if (_disposed) return;
        
        DefaultMaterialPipeline.DecrementReference();

        foreach ((_, var batches) in _batchedCandidates) {
            foreach ((_, var batched) in batches) {
                batched.Mesh.DecrementReference();
                batched.Properties.DecrementReference();
            }
        }
        _batchedCandidates.Clear();
        
        DisposeDefaultPipelines();
        DisposePipelineBatchers();
        
        _disposed = true;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~InterfaceRenderer() {
        Dispose(false);
    }
}