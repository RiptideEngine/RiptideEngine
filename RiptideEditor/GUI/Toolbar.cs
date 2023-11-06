namespace RiptideEditor;

internal static class Toolbar {
    public const float ToolbarHeight = 44;

    private static Texture2D _buttonIcons = null!;

    public static void Initialize() {
        var context = EditorApplication.RenderingContext;
        var factory = context.Factory;
        var cmdList = factory.CreateCommandList();

        using (var image = Image.Load<Rgba32>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "textures", "button_icons.png"))) {
            bool op = image.DangerousTryGetSinglePixelMemory(out var memory);
            Debug.Assert(op, "Failed to get single pixel memory from Image<Rgba32>.");

            _buttonIcons = new((ushort)image.Width, (ushort)image.Height, GraphicsFormat.R8G8B8A8UNorm);

            cmdList.UpdateResource(_buttonIcons.UnderlyingTexture, MemoryMarshal.AsBytes(memory.Span));
            cmdList.TranslateResourceStates([
                new(_buttonIcons.UnderlyingTexture, ResourceStates.CopyDestination, ResourceStates.ShaderResource),
            ]);
        }

        cmdList.Close();
        context.ExecuteCommandList(cmdList);
        context.WaitForGpuIdle();

        cmdList.DecrementReference();
    }

    public static void Render() {
        var mainViewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(mainViewport.Pos + new Vector2(0, ImGui.GetFrameHeight()));
        ImGui.SetNextWindowSize(new(mainViewport.Size.X, ToolbarHeight));
        ImGui.SetNextWindowViewport(mainViewport.ID);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.Begin("Editor Toolbar", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.PopStyleVar(2);

        if (ImGui.BeginTable("Buttons", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit)) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            DoPlaymodeButtons();

            ImGui.TableNextColumn();

            Vector2 btnSize = new(ImGui.GetFrameHeightWithSpacing());

            if (ImGui.ImageButton("Save", (nint)_buttonIcons.UnderlyingView.NativeView.Handle, btnSize, new(0f, 0.25f), new(0.25f, 0.5f))) {
                PlaymodeProcedure.Stop();
            }

            ImGui.SameLine();

            if (ImGui.ImageButton("Compile", (nint)_buttonIcons.UnderlyingView.NativeView.Handle, btnSize, new(0.25f, 0.25f), new(0.5f, 0.5f))) {
                PlaymodeProcedure.Stop();
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    public static void Shutdown() {
        _buttonIcons.DecrementReference();
    }

    private static void DoPlaymodeButtons() {
        Vector2 btnSize = new(ImGui.GetFrameHeightWithSpacing());

        var iconViewHandles = (nint)_buttonIcons.UnderlyingView.NativeView.Handle;

        if (PlaymodeProcedure.IsInPlaymode) {
            if (PlaymodeProcedure.IsPaused) {
                if (ImGui.ImageButton("Continue", iconViewHandles, btnSize, Vector2.Zero, new Vector2(0.25f, 0.25f))) {
                    PlaymodeProcedure.Unpause();
                }
            } else {
                if (ImGui.ImageButton("Pause", iconViewHandles, btnSize, new(0.25f, 0), new Vector2(0.5f, 0.25f))) {
                    PlaymodeProcedure.Pause();
                }
            }

            ImGui.SameLine();

            if (ImGui.ImageButton("Stop", iconViewHandles, btnSize, new(0.5f, 0f), new(0.75f, 0.25f))) {
                PlaymodeProcedure.Stop();
            }

            ImGui.SameLine();

            ImGui.BeginDisabled(!PlaymodeProcedure.IsPaused);
            if (ImGui.ImageButton("Next Frame", iconViewHandles, btnSize, new(0.75f, 0f), new(1f, 0.25f))) {
            }
            ImGui.EndDisabled();
        } else {
            if (ImGui.ImageButton("Begin", iconViewHandles, btnSize, Vector2.Zero, new Vector2(0.25f, 0.25f))) {
                PlaymodeProcedure.Begin();
            }

            ImGui.BeginDisabled(true);

            ImGui.SameLine();
            ImGui.ImageButton("Stop", iconViewHandles, btnSize, new(0.5f, 0f), new(0.75f, 0.25f));

            ImGui.SameLine();
            ImGui.ImageButton("Next Frame", iconViewHandles, btnSize, new(0.75f, 0f), new(1f, 0.25f));

            ImGui.EndDisabled();
        }
    }
}