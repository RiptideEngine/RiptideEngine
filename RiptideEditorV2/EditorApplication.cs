using Riptide.LowLevel.TextEngine;
using Riptide.ShaderCompilation;
using RiptideEditorV2.UI;
using RiptideEngine.Core.Allocation;
using RiptideFoundation.Helpers;
using RiptideFoundation.Text;
using RiptideFoundation.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;
using Button = Silk.NET.Input.Button;

namespace RiptideEditorV2;

public static unsafe partial class EditorApplication {
    private static RenderingContext _renderContext = null!;

    public static InterfaceDocument _ui = null!;

    internal static void Initialize() {
        CreateWindow();
    }

    private static void Init() {
        InitializeServices();
        _renderContext = _services.GetRequiredService<IRenderingService>().Context;
        
        Graphics.Initialize(_services);
        RuntimeFoundation.Initialize(_services);
        FontEngine.Initialize();

        _ui = new(_renderContext) {
            DisplaySize = new(_window.Size.X, _window.Size.Y),
        };
        CreateDefaultUIMaterials(_ui);

        var button = new RiptideEditorV2.UI.Button {
            Name = "Button",
            Layout = {
                X = 10,
                Y = 10,
                Width = 400,
                Height = 400,
            },
            BorderRadii = new(100),
        };
        button.SetParent(_ui.Root);

        _services.GetRequiredService<IInputService>().KeyDown += (key) => {
            RiptideEditorV2.UI.Button? button;
            
            switch (key) {
                case Key.Up:
                    if ((button = _ui.Root.Search<RiptideEditorV2.UI.Button>("Button")) != null) {
                        button.BorderRadii -= new Vector4(10);
                    }
                    break;
                
                case Key.Down:
                    if ((button = _ui.Root.Search<RiptideEditorV2.UI.Button>("Button")) != null) {
                        button.BorderRadii += new Vector4(10);
                    }
                    break;
            }
        };
    }

    private static void Update(double dt) {
        // if (_ui.Root.Search<RiptideEditorV2.UI.Button>("Button") is { } button) {
        //     button.BorderRadii = new(float.FusedMultiplyAdd(float.Sin((float)_window.Time * 4), 0.5f, 0.5f) * 200);
        // }
        
        _ui.Update();
    }
    
    private static void Render(double dt) {
        var cmdList = _renderContext.Factory.CreateCommandList();

        (var rtvResource, var rtvView) = _renderContext.SwapchainCurrentRenderTarget;
        
        cmdList.TranslateResourceState(rtvResource, ResourceTranslateStates.RenderTarget);
        
        cmdList.SetRenderTarget(rtvView, null);
        cmdList.ClearRenderTarget(rtvView, new(0.07f));
        
        _ui.Render(cmdList);
        
        cmdList.TranslateResourceState(rtvResource, ResourceTranslateStates.Present);
        
        cmdList.Close();
        _renderContext.ExecuteCommandList(cmdList);
        cmdList.DecrementReference();
        
        _renderContext.Present();
    }

    private static void Resize(Vector2D<int> newSize) {
        if (newSize.X == 0 || newSize.Y == 0) return;
        
        _renderContext.ResizeSwapchain((uint)newSize.X, (uint)newSize.Y);
        
        _ui.DisplaySize = new(newSize.X, newSize.Y);
    }
    
    private static void Shutdown() {
        _ui.Dispose();
        
        FontEngine.Shutdown();
        RuntimeFoundation.Shutdown();
        Graphics.Shutdown();
        
        _services.RemoveAllServices();
    }

    public static void CreateDefaultUIMaterials(InterfaceDocument document) {
        var context = Services.GetRequiredService<IRenderingService>().Context;
        var factory = context.Factory;
        
        GraphicalShader shader;
        CompactedShaderReflection reflection;
        
        var compiler = Graphics.CompilationFactory.CreateCompilationPipeline();
        
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RiptideEditorV2.Resources.Shaders.SDF-Font.hlsl")!) {
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
        
            vsReflection.DecrementReference();
            psReflection.DecrementReference();
            
            vsResult.DecrementReference();
            psResult.DecrementReference();
        }
        
        RenderTargetFormats formats = default;
        formats[0] = GraphicsFormat.R8G8B8A8UNorm;
        
        var material = new MaterialPipeline(shader, reflection, new() {
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
                    Filter = SamplerFilter.Linear,
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
            Name = "TextElement's Default Material",
        };
        
        shader.DecrementReference();
        
        document.Renderer.SetDefaultPipeline<TextElement>(material);

        material.DecrementReference();
    }
}