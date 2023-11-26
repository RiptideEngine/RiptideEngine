namespace RiptideEditor.Application;

internal sealed class ProjectCreationState : ApplicationState {
    public ProjectCreator Creator { get; set; } = null!;

    public override void Begin() {
    }

    public override void Update() {
    }

    public override void RenderGUI() {
        var viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.Begin("ProjectCreationState_MainWorkarea", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
        ImGui.PopStyleVar();

        ImGui.Text("Huehuehue");

        ImGui.End();
    }

    public override void End() {
    }
}