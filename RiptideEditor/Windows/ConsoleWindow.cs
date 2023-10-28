namespace RiptideEditor.Windows;

public sealed class ConsoleWindow : EditorWindow {
    public override bool Render() {
        bool open = true;

        ImGui.SetNextWindowSize(new(400, 300), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Console", ref open, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
            ImGui.BeginGroup();
            {
                //foreach (var record in EditorApplication.Logger.EnumerateRecords()) {
                //    switch (record.Type) {
                //        case LoggingType.Info:
                //            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFFFFF);
                //            ImGui.Text(record.Message);
                //            ImGui.PopStyleColor();
                //            break;

                //        case LoggingType.Warning:
                //            ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
                //            ImGui.Text(record.Message);
                //            ImGui.PopStyleColor();
                //            break;

                //        case LoggingType.Error:
                //            ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
                //            ImGui.Text(record.Message);
                //            ImGui.PopStyleColor();
                //            break;
                //    }
                //}
            }
            ImGui.EndGroup();

            ImGui.End();
        }

        return open;
    }

    [MenuBarCallback(MenuBarSection.View, "Editor/Console")]
    private static void SummonWindow() {
        EditorWindows.GetOrAddWindowInstance<ConsoleWindow>();
    }
}