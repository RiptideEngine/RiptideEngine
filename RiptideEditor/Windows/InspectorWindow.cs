namespace RiptideEditor.Windows;

public partial class InspectorWindow : EditorWindow {
    public InspectorWindow() {
        Selections.OnSelectionChanged += OnSelectionChanged;
    }

    public override bool Render() {
        bool open = true;

        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Inspector", ref open)) {
            int multipleCounter = 0;

            int numEntities = Selections.NumSelectedEntities, numResources = Selections.NumSelectedGuids;

            multipleCounter += Unsafe.BitCast<bool, byte>(numEntities > 0);
            multipleCounter += Unsafe.BitCast<bool, byte>(numResources > 0);

            switch (multipleCounter) {
                case 1:
                    if (numEntities > 0) {
                        DoEntityInspecting();
                    } else if (numResources > 0) {
                        //DoResourceInspecting();
                    }
                    break;

                case 0: break;

                default:
                    ImGui.TextUnformatted("Cannot inspecting multiple types of selected objects.");

                    if (numEntities > 0) {
                        ImGui.BulletText($"{numEntities} scene {(numEntities == 1 ? "entity" : "entities")}.");
                    }

                    if (numResources > 0) {
                        ImGui.BulletText($"{numResources} scene {(numResources == 1 ? "asset" : "assets")}.");
                    }
                    break;
            }

            ImGui.End();
        }

        return open;
    }

    protected override void OnDispose(bool disposeManaged) {
        base.OnDispose(disposeManaged);

        Selections.OnSelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(SelectionChangedType type, SelectionChangedTarget target) {
        switch (target) {
            case SelectionChangedTarget.SceneEntity:
                OnEntitySelectionChanged();
                break;

            case SelectionChangedTarget.Resource:
                //OnResourceSelectionChanged();
                break;
        }
    }

    [MenuBarCallback(MenuBarSection.View, "Editor/Inspector")]
    private static void SummonWindow() {
        EditorWindows.GetOrAddWindowInstance<InspectorWindow>();
    }

    private readonly record struct ComponentDrawingElement(string ID, BaseComponentDrawer Drawer, Component[] Targets);
}