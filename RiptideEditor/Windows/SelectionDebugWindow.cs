namespace RiptideEditor.Windows;

public sealed class SelectionDebugWindow : EditorWindow {
    private readonly StringBuilder _builder = new();

    public override bool Render() {
        bool open = true;

        ImGui.SetNextWindowSize(new(400, 300), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Selection Debug", ref open, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
            if (ImGui.BeginTabBar("Section Tab")) {
                if (ImGui.BeginTabItem("Entity")) {
                    _builder.Clear();

                    int i = 0;
                    foreach (var entity in Selections.EnumerateSelectedEntities()) {
                        _builder.Append(i + 1).Append(". ").Append(entity.Name ?? "<Unnamed Entity>").Append(" (").Append(entity.Scene.Name ?? "<Unnamed Scene>").AppendLine(")");

                        i++;
                    }

                    ImGui.TextUnformatted(_builder.ToString());

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Resource")) {
                    _builder.Clear();

                    int i = 0;
                    foreach (var rid in Selections.EnumerateSelectedResourceIDs()) {
                        _builder.Append(i + 1).Append(". ").Append(rid);
                        
                        if (EditorResourceDatabase.TryConvertResourceIDToPath(rid, out var path)) {
                            _builder.Append(" (").Append(path).AppendLine(")");
                        } else {
                            _builder.Append(" (Unknown path, definitely an error)");
                        }

                        i++;
                    }

                    ImGui.TextUnformatted(_builder.ToString());

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        return open;
    }

    [MenuBarCallback(MenuBarSection.View, "Editor/Debug/Selection")]
    private static void SummonWindow() {
        EditorWindows.GetOrAddWindowInstance<SelectionDebugWindow>();
    }
}