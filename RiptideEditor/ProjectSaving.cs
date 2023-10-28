namespace RiptideEditor;

public static class ProjectSaving {
    public static event Action? SaveOperation;

    [Shortcut("Save", "Ctrl+S", ExecutionMode = ShortcutExecutionMode.Single)]
    private static void Save() {
        if (ImGui.IsAnyItemActive()) return;

        SaveOperation?.Invoke();
    }
}