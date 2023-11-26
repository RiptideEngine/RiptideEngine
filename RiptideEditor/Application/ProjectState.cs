namespace RiptideEditor.Application;

internal sealed class ProjectState : ApplicationState {
    public override void Begin() {
        
    }

    public override void RenderGUI() {
        var mainViewport = ImGui.GetMainViewport();

        // MenuBar.Render();

        // Toolbar.Render();

        ImGui.SetNextWindowPos(mainViewport.Pos + new Vector2(0, ImGui.GetFrameHeight() + Toolbar.ToolbarHeight));
        ImGui.SetNextWindowSize(new(mainViewport.Size.X, mainViewport.Size.Y - ImGui.GetFrameHeight() - Toolbar.ToolbarHeight));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Main Workspace", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        ImGui.PopStyleVar(3);
        {
            ImGui.DockSpace(ImGui.GetID("Main Dockspace"), ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin(), ImGuiDockNodeFlags.PassthruCentralNode);

            // EditorWindows.RenderWindows();
        }
        ImGui.End();
    }

    public override void Update() {
        
    }

    public override void End() {
    }
}