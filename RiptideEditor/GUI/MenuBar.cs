namespace RiptideEditor;

internal static class MenuBar {
    private static readonly (MenuBarSection Value, string Name)[] _menubarSections = null!;

    static MenuBar() {
        _menubarSections = Enum.GetValues<MenuBarSection>().Zip(Enum.GetNames<MenuBarSection>(), (first, second) => (first, second)).ToArray();

        MenuBarDatabase.Reload();
    }

    public static void Render() {
        if (ImGui.BeginMainMenuBar()) {
            Span<Range> splitRanges = stackalloc Range[2];

            foreach ((var section, var sectionName) in _menubarSections) {
                if (ImGui.BeginMenu(sectionName)) {
                    var entries = MenuBarDatabase.GetEntries(section);

                    foreach (var entry in entries) {
                        RenderElement(entry.Path, entry.Shortcut, splitRanges, entry);
                    }

                    ImGui.EndMenu();
                }
            }

            ImGui.EndMainMenuBar();
        }

        static void RenderElement(ReadOnlySpan<char> path, ReadOnlySpan<char> shortcut, Span<Range> ranges, MenuBarDatabase.Entry entry) {
            int numSplit = path.Split(ranges, '/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Debug.Assert(numSplit != 0);

            switch (numSplit) {
                case 2:
                    if (ImGui.BeginMenu(path[ranges[0]])) {
                        RenderElement(path[ranges[1]], shortcut, ranges, entry);

                        ImGui.EndMenu();
                    }
                    break;

                case 1:
                    bool visibility = entry.VisibilityCallback?.Invoke() ?? true;

                    if (ImGui.MenuItem(path[ranges[0]], shortcut, false, visibility)) {
                        entry.Callback.Invoke();
                    }
                    return;
            }
        }
    }
}