using RiptideRendering.Shadering;

namespace RiptideEditor.Windows;

public sealed unsafe class SceneViewWindow : EditorWindow {
    private readonly ISceneGraphService _sceneService;
    private readonly IInputService _inputService;

    private RenderTarget _gameRenderTarget = null!;
    private DepthTexture _gameDepthTexture = null!;

    private Vector2 _lastContentSize = default;

    private ulong _resourceRecreationFrame = 0;

    private RenderTarget _pickingIDBuffer = null!;
    private DepthTexture _pickingDepthBuffer = null!;
    private GraphicalShader _pickingShader;
    private PipelineState _pickingPSO;

    private ReadbackBuffer _pickingReadback = null!;

    public SceneViewWindow() {
        _sceneService = EditorApplication.Services.GetRequiredService<ISceneGraphService>();
        _inputService = EditorApplication.Services.GetRequiredService<IInputService>();

        var source = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "NodeIDWriter.hlsl"));

        using ComPtr<IDxcBlob> pVSBytecode = default;
        using ComPtr<IDxcBlob> pRSBytecode = default;
        using ComPtr<IDxcBlob> pPSBytecode = default;

        ShaderCompileUtils.CompileShader(source, "vs_6_0\0", "vsmain\0", "-O3\0", default, pVSBytecode.GetAddressOf(), pRSBytecode.GetAddressOf());
        ShaderCompileUtils.CompileShader(source, "ps_6_0\0", "psmain\0", "-O3\0", default, pPSBytecode.GetAddressOf(), null);

        _pickingShader = EditorApplication.RenderingContext.Factory.CreateGraphicalShader(pVSBytecode.AsSpan(), pPSBytecode.AsSpan(), default, default, pRSBytecode.AsSpan());

        RenderTargetFormats formats = default;
        formats[0] = GraphicsFormat.R32UInt;
        _pickingPSO = EditorApplication.RenderingContext.Factory.CreatePipelineState(_pickingShader, new PipelineStateConfig() {
            Blending = BlendingConfig.Disable,
            DepthStencil = new() {
                EnableDepth = true,
                EnableStencil = true,
                DepthFunction = ComparisonFunction.Less,
                BackfaceOperation = new() {
                    DepthFailOp = StencilOperation.Decr,
                    FailOp = StencilOperation.Keep,
                    PassOp = StencilOperation.Keep,
                    Function = ComparisonFunction.Always,
                },
                FrontFaceOperation = new() {
                    DepthFailOp = StencilOperation.Incr,
                    FailOp = StencilOperation.Keep,
                    PassOp = StencilOperation.Keep,
                    Function = ComparisonFunction.Always,
                },
            },
            Rasterization = new() {
                Conservative = false,
                CullMode = CullingMode.None,
                FillMode = FillingMode.Solid,
            },
            RenderTargetFormats = new() {
                NumRenderTargets = 1,
                Formats = formats,
            },
        });

        _pickingReadback = EditorApplication.RenderingContext.Factory.CreateReadbackBuffer(new() {
            Size = 4,
        });
    }

    public override bool Render() {
        bool open = true;

        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.FirstUseEver);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        bool render = ImGui.Begin("Scene View", ref open, ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        ImGui.PopStyleVar();

        if (render) {
            //var context = EditorApplication.RenderingContext;
            //var factory = context.Factory;
            //var currentContentRegionSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

            //if (currentContentRegionSize.X != 0 && currentContentRegionSize.Y != 0) {
            //    if (_gameRenderTarget == null || _gameDepthTexture == null || _pickingIDBuffer == null || _pickingDepthBuffer == null) {                    
            //        _lastContentSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

            //        RecreateOutputResources((uint)_lastContentSize.X, (uint)_lastContentSize.Y);

            //        _resourceRecreationFrame = EditorApplication.Time.ElapsedFrames;
            //    } else {
            //        if (Vector2.DistanceSquared(_lastContentSize, currentContentRegionSize) >= 0.01f) {
            //            _lastContentSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

            //            _gameRenderTarget!.DecrementReference();
            //            _gameDepthTexture!.DecrementReference();
            //            _pickingIDBuffer!.DecrementReference();
            //            _pickingDepthBuffer!.DecrementReference();

            //            RecreateOutputResources((uint)_lastContentSize.X, (uint)_lastContentSize.Y);

            //            _resourceRecreationFrame = EditorApplication.Time.ElapsedFrames;
            //        }
            //    }

            //    Debug.Assert(_gameRenderTarget != null && _gameDepthTexture != null && _pickingIDBuffer != null && _pickingDepthBuffer != null); ;

            //    var cmdList = factory.CreateCommandList();

            //    cmdList.TranslateResourceStates(stackalloc ResourceTransitionDescriptor[] {
            //        new(_gameRenderTarget.ResourceHandle, ResourceStates.ShaderResource, ResourceStates.RenderTarget),
            //        new(_gameDepthTexture.ResourceHandle, ResourceStates.DepthRead, ResourceStates.DepthWrite),
            //    });

            //    cmdList.SetViewport(new(0, 0, _lastContentSize.X, _lastContentSize.Y));
            //    cmdList.SetScissorRect(new(0, 0, (int)_lastContentSize.X, (int)_lastContentSize.Y));

            //    // Render the game scene
            //    var viewMatrix = Matrix4x4.CreateLookToLeftHanded(_sceneCameraPosition, Vector3.Transform(Vector3.UnitZ, _sceneCameraRotation), Vector3.Transform(Vector3.UnitY, _sceneCameraRotation));
            //    var projMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(70f / 180f * float.Pi, _lastContentSize.X / _lastContentSize.Y, 0.01f, 100f);

            //    // _renderingLayer.Render(cmdList, viewMatrix, projMatrix, _gameRenderTarget, _gameDepthTexture);

            //    ImGui.Image((nint)_gameRenderTarget.ViewHandle.Handle, _lastContentSize);

            //    cmdList.TranslateResourceStates(stackalloc ResourceTransitionDescriptor[] {
            //        new(_gameRenderTarget.ResourceHandle, ResourceStates.RenderTarget, ResourceStates.ShaderResource),
            //        new(_gameDepthTexture.ResourceHandle, ResourceStates.DepthWrite, ResourceStates.DepthRead),
            //    });

            //    bool clickScene = ImGui.IsItemClicked(ImGuiMouseButton.Left);

            //    //if (clickScene) {
            //    //    cmdList.TranslateResourceStates(stackalloc ResourceTransitionDescriptor[] {
            //    //        new(_pickingIDBuffer!, ResourceStates.CopySource, ResourceStates.RenderTarget),
            //    //        new(_pickingDepthBuffer!, ResourceStates.DepthRead, ResourceStates.DepthWrite),
            //    //    });

            //    //    cmdList.ClearRenderTarget(_pickingIDBuffer, Vector4.Zero);
            //    //    cmdList.ClearDepthTexture(_pickingDepthBuffer, DepthTextureClearFlags.All, 1, 0);
            //    //    cmdList.SetRenderTarget(_pickingIDBuffer, _pickingDepthBuffer);

            //    //    cmdList.SetPipelineState(_pickingPSO);
            //    //    cmdList.SetGraphicsBindingSchematic(_pickingShader);

            //    //    var viewProjection = viewMatrix * projMatrix;

            //    //    foreach (var scene in _sceneService.SceneContext.EnumerateScenes()) {
            //    //        foreach (var node in scene.EnumerateRootEntities()) {
            //    //            var mvp = node.LocalToWorldMatrix * viewProjection;

            //    //            cmdList.SetGraphicsDynamicConstantBuffer("_Transformation", new(&mvp, 64));

            //    //            uint id = node.ID;
            //    //            cmdList.SetGraphicsDynamicConstantBuffer("_RootConstants", new(&id, 4));

            //    //            cmdList.SetGraphicsReadonlyBuffer("_RIPTIDE_VERTEXBUFFER_C0", _renderingLayer.Quad.GetVertexBuffer(0)!, 0, GraphicsFormat.Unknown);
            //    //            cmdList.SetIndexBuffer(_renderingLayer.Quad.IndexBuffer!, IndexFormat.UInt16, 0);
            //    //            cmdList.DrawIndexed(_renderingLayer.Quad.IndexCount, 1, 0, 0);
            //    //        }
            //    //    }

            //    //    cmdList.TranslateResourceStates(stackalloc ResourceTransitionDescriptor[] {
            //    //        new(_pickingIDBuffer, ResourceStates.RenderTarget, ResourceStates.CopySource),
            //    //        new(_pickingDepthBuffer, ResourceStates.DepthWrite, ResourceStates.DepthRead),
            //    //    });

            //    //    var readPosition = ImGui.GetMousePos() - (ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin());

            //    //    cmdList.ReadTexture(_pickingIDBuffer, new Box3D<uint>((uint)readPosition.X, (uint)readPosition.Y, 0, (uint)readPosition.X + 1, (uint)readPosition.Y + 1, 1), _pickingReadback);
            //    //}

            //    cmdList.Close();
            //    context.ExecuteCommandList(cmdList);
            //    context.WaitForGpuIdle();

            //    cmdList.DecrementReference();

            //    if (clickScene) {
            //        var span = _pickingReadback.GetMappedData();

            //        var search = _sceneService.SceneContext.SearchEntity(MemoryMarshal.Cast<byte, uint>(span)[0]);

            //        if (search != null) {
            //            NodeSelection.DeselectAll();
            //            NodeSelection.Select(search);
            //        }
            //    }
            //}

            //if (ImGui.IsWindowFocused()) {
            //    var dt = EditorApplication.Time.DeltaTime;

            //    if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle)) {
            //        _cameraRot += ImGui.GetMouseDragDelta(ImGuiMouseButton.Middle) * dt * 0.25f;

            //        ImGui.ResetMouseDragDelta(ImGuiMouseButton.Middle);
            //    }

            //    _sceneCameraRotation = Quaternion.CreateFromYawPitchRoll(-_cameraRot.X, -_cameraRot.Y, 0);
            //    _sceneCameraPosition += Vector3.Transform(new Vector3(_inputService.GetAxis(Key.A, Key.D), _inputService.GetAxis(Key.ShiftLeft, Key.Space), _inputService.GetAxis(Key.S, Key.W)) * 4 * dt, _sceneCameraRotation);
            //}

            ImGui.End();
        }

        return open;
    }

    private void RecreateOutputResources(uint width, uint height) {
        var factory = EditorApplication.RenderingContext.Factory;

        _gameRenderTarget = factory.CreateRenderTarget(new() {
            Width = width,
            Height = height,
            Format = GraphicsFormat.R8G8B8A8UNorm,
            InitialStates = ResourceStates.ShaderResource,
        });
        _gameRenderTarget.Name = $"{GetType().Name}.{nameof(_gameRenderTarget)}";

        _gameDepthTexture = factory.CreateDepthTexture(new() {
            Width = width,
            Height = height,
            Format = GraphicsFormat.D24UNormS8UInt,
            InitialStates = ResourceStates.DepthRead,
        });
        _gameDepthTexture.Name = $"{GetType().Name}.{nameof(_gameDepthTexture)}";

        _pickingIDBuffer = factory.CreateRenderTarget(new() {
            Width = width,
            Height = height,
            Format = GraphicsFormat.R32UInt,
            InitialStates = ResourceStates.CopySource,
        });
        _pickingIDBuffer.Name = $"{GetType().Name}.{nameof(_pickingIDBuffer)}";

        _pickingDepthBuffer = factory.CreateDepthTexture(new() {
            Width = width,
            Height = height,
            Format = GraphicsFormat.D24UNormS8UInt,
            InitialStates = ResourceStates.DepthRead,
        });
        _pickingDepthBuffer.Name = $"{GetType().Name}.{nameof(_pickingDepthBuffer)}";
    }

    protected override void OnDispose(bool disposeManaged) {
        _pickingIDBuffer?.DecrementReference();
        _pickingPSO?.DecrementReference();
        _pickingShader?.DecrementReference();
        _pickingDepthBuffer?.DecrementReference();
        _pickingReadback.DecrementReference();

        _gameRenderTarget?.DecrementReference();
        _gameDepthTexture?.DecrementReference();
    }

    [MenuBarCallback(MenuBarSection.View, "Editor/Scene View")]
    private static void SummonWindow() {
        EditorWindows.GetOrAddWindowInstance<SceneViewWindow>();
    }
}