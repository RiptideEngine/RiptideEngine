namespace RiptideEditor.Windows;

internal sealed unsafe class GameViewWindow : EditorWindow {
    private RenderTarget _gameOutput = null!;

    public GameViewWindow() { }

    public override bool Render() {
        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.FirstUseEver);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        bool open = true;
        bool render = ImGui.Begin("Game View", ref open, ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        ImGui.PopStyleVar();

        if (render) {
            //var containerSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            //var textureSize = CalculateGameDisplaySize(containerSize, 16f / 9f);

            //int textureSizeX = (int)textureSize.X, textureSizeY = (int)textureSize.Y;

            //if (textureSizeX <= 0 || textureSizeY <= 0) {
            //    _gameOutput?.DecrementReference();
            //} else {
            //    var textureCenter = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin() + containerSize / 2;

            //    var context = EditorApplication.RenderingContext;
            //    var factory = context.Factory;

            //    CommandList cmdList;

            //    if ((textureSize - GameRuntimeContext.WindowService.Size).LengthSquared() >= 0.001f) {
            //        _gameOutput?.DecrementReference();

            //        uint textureWidth = (uint)textureSize.X, textureHeight = (uint)textureSize.Y;

            //        _gameOutput = new(TextureDimension.Texture2D, (uint)textureSize.X, (ushort)textureSize.Y, 1, GraphicsFormat.R8G8B8A8UNorm, GraphicsFormat.D24UNormS8UInt) {
            //            Name = "GameViewWindow._gameRenderTexture"
            //        };

            //        GameRuntimeContext.WindowService.Size = textureSize;
            //    } else {
            //        cmdList = factory.CreateCommandList();

            //        cmdList.TranslateResourceStates([
            //            new(_gameOutput.UnderlyingTexture, ResourceStates.ShaderResource, ResourceStates.RenderTarget),
            //            new(_gameOutput.UnderlyingDepthTexture!, ResourceStates.DepthRead, ResourceStates.DepthWrite),
            //        ]);

            //        cmdList.Close();
            //        Graphics.AddCommandListExecutionBatch(cmdList);
            //        cmdList.DecrementReference();
            //    }

            //    //Graphics.RenderingPipeline.ExecuteRenderingOperation(new() {
            //    //    OutputCameras = EditorScene.EnumerateScenes().SelectMany(x => x.EnumerateRootEntities()).SelectMany(x => IterateOperation.IterateDownward(x).Prepend(x)).Select(x => x.GetComponent<Camera>()).Where(x => x != null).ToArray()!,
            //    //    OutputTarget = _gameOutput,
            //    //});

            //    cmdList = factory.CreateCommandList();

            //    cmdList.TranslateResourceStates([
            //        new(_gameOutput.UnderlyingTexture, ResourceStates.RenderTarget, ResourceStates.ShaderResource),
            //        new(_gameOutput.UnderlyingDepthTexture!, ResourceStates.DepthWrite, ResourceStates.DepthRead),
            //    ]);

            //    cmdList.Close();
            //    Graphics.AddCommandListExecutionBatch(cmdList);
            //    cmdList.DecrementReference();

            //    Graphics.FlushCommandListExecutionBatch();

            //    ImGui.SetCursorPos(ImGui.GetCursorPos() + (containerSize - textureSize) / 2);
            //    ImGui.Image((nint)_gameOutput.UnderlyingTexture.NativeResource.Handle, textureSize);
            //}

            ImGui.End();
        }

        return open;
    }

    private static Vector2 CalculateGameDisplaySize(Vector2 containerSize, float aspectRatio) {
        if (containerSize.X >= containerSize.Y * aspectRatio) {
            return new Vector2(containerSize.Y * aspectRatio, containerSize.Y);
        } else {
            return new Vector2(containerSize.X, containerSize.X / aspectRatio);
        }
    }

    protected override void OnDispose(bool disposeManaged) {
        _gameOutput?.DecrementReference();
    }

    [MenuBarCallback(MenuBarSection.View, "Editor/Game View")]
    private static void SummonWindow() {
        EditorWindows.GetOrAddWindowInstance<GameViewWindow>();
    }
}