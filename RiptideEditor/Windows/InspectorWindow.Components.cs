namespace RiptideEditor.Windows;

partial class InspectorWindow {
    private readonly List<SingularEntityComponentDrawer> _singularEntityDrawers = [];

    private void OnEntitySelectionChanged() {
        RecreateComponentDrawers();
    }

    private void RecreateComponentDrawers() {
        _singularEntityDrawers.Clear();

        if (Selections.NumSelectedEntities == 1) {
            StringBuilder sb = new();
            Span<char> guidBuffer = stackalloc char[32];

            foreach (var component in Selections.EnumerateSelectedEntities().First().EnumerateComponents()) {
                var componentType = component.GetType();

                if (!ComponentDrawerDatabase.TryCreateDrawer(componentType, out var drawer)) continue;

                sb.Append(componentType.Name).Append('#', 2);

                Guid.NewGuid().TryFormat(guidBuffer, out _, "N");

                sb.Append(guidBuffer);

                _singularEntityDrawers.Add(new(sb.ToString(), drawer, component));

                sb.Clear();
            }
        }
    }

    private void AddComponentSingleEntityDrawer(Component component) {
        var componentType = component.GetType();

        if (!ComponentDrawerDatabase.TryCreateDrawer(componentType, out var drawer)) return;

        StringBuilder sb = new();
        Span<char> guidBuffer = stackalloc char[32];

        sb.Append(componentType.Name).Append('#', 2);

        Guid.NewGuid().TryFormat(guidBuffer, out _, "N");

        sb.Append(guidBuffer);

        _singularEntityDrawers.Add(new(sb.ToString(), drawer, component));

        sb.Clear();
    }

    private void DoComponentDrawing() {
        if (_singularEntityDrawers.Count != 0) {
            foreach (var entry in _singularEntityDrawers) {
                if (ImGui.CollapsingHeader(entry.ID)) {
                    entry.Drawer.TargetComponent = entry.Component;
                    entry.Drawer.Render();
                }
            }
        }
    }

    private readonly record struct SingularEntityComponentDrawer(string ID, BaseComponentDrawer Drawer, Component Component);
}