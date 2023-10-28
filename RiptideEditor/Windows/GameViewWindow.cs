namespace RiptideEditor.Windows;

internal sealed unsafe class GameViewWindow : EditorWindow {
    private RenderTarget _gameRenderTexture = null!;
    private DepthTexture _gameDepthTexture = null!;

    public GameViewWindow() { }

    public override bool Render() {
        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.FirstUseEver);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        bool open = true;
        bool render = ImGui.Begin("Game View", ref open, ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        ImGui.PopStyleVar();

        if (render) {
            var containerSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var textureSize = CalculateGameDisplaySize(containerSize, 16f / 9f);

            int textureSizeX = (int)textureSize.X, textureSizeY = (int)textureSize.Y;

            if (textureSizeX <= 0 || textureSizeY <= 0) {
                _gameRenderTexture?.DecrementReference();
                _gameDepthTexture?.DecrementReference();
            } else {
                var textureCenter = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin() + containerSize / 2;

                var context = EditorApplication.RenderingContext;
                var factory = context.Factory;

                CommandList cmdList;

                if ((textureSize - GameRuntimeContext.WindowService.Size).LengthSquared() >= 0.001f) {
                    _gameRenderTexture?.DecrementReference();
                    _gameDepthTexture?.DecrementReference();

                    uint textureWidth = (uint)textureSize.X, textureHeight = (uint)textureSize.Y;

                    _gameRenderTexture = factory.CreateRenderTarget(new() {
                        Width = textureWidth,
                        Height = textureHeight,
                        Format = GraphicsFormat.R8G8B8A8UNorm,
                        InitialStates = ResourceStates.RenderTarget,
                    });
                    _gameDepthTexture = factory.CreateDepthTexture(new() {
                        Width = textureWidth,
                        Height = textureHeight,
                        Format = GraphicsFormat.D24UNormS8UInt,
                        InitialStates = ResourceStates.DepthWrite,
                    });

                    _gameRenderTexture.Name = "GameViewWindow._gameRenderTexture";
                    _gameDepthTexture.Name = "GameViewWindow._gameDepthTexture";

                    GameRuntimeContext.WindowService.Size = textureSize;
                } else {
                    cmdList = factory.CreateCommandList();

                    cmdList.TranslateResourceStates([
                        new(_gameRenderTexture, ResourceStates.ShaderResource, ResourceStates.RenderTarget),
                        new(_gameDepthTexture, ResourceStates.DepthRead, ResourceStates.DepthWrite),
                    ]);

                    cmdList.Close();
                    Graphics.AddCommandListExecutionBatch(cmdList);
                    cmdList.DecrementReference();
                }

                Graphics.RenderingPipeline.ExecuteRenderingOperation(new() {
                    OutputCameras = EditorScene.EnumerateEditorScenes().SelectMany(x => x.EnumerateRootEntities()).SelectMany(x => IterateOperation.IterateDownward(x).Prepend(x)).Select(x => x.GetComponent<Camera>()).Where(x => x != null).ToArray()!,
                    OutputRenderTarget = _gameRenderTexture,
                    OutputDepthTexture = _gameDepthTexture,
                });

                cmdList = factory.CreateCommandList();

                cmdList.TranslateResourceStates([
                    new(_gameRenderTexture, ResourceStates.RenderTarget, ResourceStates.ShaderResource),
                    new(_gameDepthTexture, ResourceStates.DepthWrite, ResourceStates.DepthRead),
                ]);

                cmdList.Close();
                Graphics.AddCommandListExecutionBatch(cmdList);
                cmdList.DecrementReference();

                Graphics.FlushCommandListExecutionBatch();

                ImGui.SetCursorPos(ImGui.GetCursorPos() + (containerSize - textureSize) / 2);
                ImGui.Image((nint)_gameRenderTexture.ViewHandle.Handle, textureSize);
            }

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
        _gameRenderTexture?.DecrementReference();
        _gameDepthTexture?.DecrementReference();
    }

    [MenuBarCallback(MenuBarSection.View, "Editor/Game View")]
    private static void SummonWindow() {
        EditorWindows.GetOrAddWindowInstance<GameViewWindow>();
    }
}