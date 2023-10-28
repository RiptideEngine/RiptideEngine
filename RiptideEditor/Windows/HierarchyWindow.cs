namespace RiptideEditor;

public sealed unsafe class HierarchyWindow : EditorWindow {
    private static ReadOnlySpan<char> EntityDragDropID => "DRAGGING_ENTITIES";
    private static ReadOnlySpan<byte> UnsavedSceneCloseModal => "Close Unsaved Scene\0"u8;

    private Scene _enumeratingScene;
    private Scene? _closingScene;

    public HierarchyWindow() {
        _enumeratingScene = null!;
    }

    public override bool Render() {
        bool open = true;

        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.FirstUseEver);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 4));
        var window = ImGui.Begin("Hierarchy", ref open);
        ImGui.PopStyleVar();

        if (window) {
            int index = 0;

            if (ImGui.BeginPopupContextWindow("Void popup context", ImGuiPopupFlags.MouseButtonRight)) {
                if (ImGui.MenuItem("Create New Scene")) {
                    EditorScene.CreateScene();
                }

                ImGui.EndPopup();
            }

            ImGui.BeginGroup();

            foreach (var scene in EditorScene.EnumerateEditorScenes()) {
                _enumeratingScene = scene;

                ImGui.PushID(index++);
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));

                    bool expand;
                    bool openscene = true;

                    if (EditorScene.IsSceneDirty(scene)) {
                        expand = ImGui.CollapsingHeader(string.IsNullOrEmpty(scene.Name) ? "<Unnamed Scene> (Unsaved)" : scene.Name + " (Unsaved)", ref openscene, ImGuiTreeNodeFlags.FramePadding);
                    } else {
                        expand = ImGui.CollapsingHeader(string.IsNullOrEmpty(scene.Name) ? "<Unnamed Scene>" : scene.Name, ref openscene, ImGuiTreeNodeFlags.FramePadding);
                    }
                    ImGui.PopStyleVar(2);

                    if (ImGui.BeginPopupContextItem("Scene Context Menu", ImGuiPopupFlags.MouseButtonRight)) {
                        if (ImGui.MenuItem("Create Entity")) {
                            scene.CreateEntity();
                        }

                        if (ImGui.MenuItem("Mark Dirty")) {
                            EditorScene.MarkSceneDirty(scene);
                        }

                        if (ImGui.MenuItem("Test Serialize")) {
                            EditorScene.TestSerializeScene(scene);
                        }

                        ImGui.EndPopup();
                    }

                    ImGui.PushID("Scene Entities");

                    if (expand) {
                        DrawSceneHierarchy(scene);
                    }

                    ImGui.PopID();

                    if (!openscene) {
                        _closingScene = scene;
                    }

                    ImGui.Spacing();
                }

                ImGui.PopID();
            }

            ImGui.EndGroup();

            if (_closingScene != null) {
                if (EditorScene.IsSceneDirty(_closingScene)) {
                    fixed (byte* pModal = UnsavedSceneCloseModal) {
                        if (ImGuiNative.igIsPopupOpen_Str(pModal, ImGuiPopupFlags.None) == 0) {
                            ImGuiNative.igOpenPopup_Str(pModal, ImGuiPopupFlags.None);
                        }

                        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Always, new Vector2(0.5f, 0.5f));

                        if (ImGuiNative.igBeginPopupModal(pModal, null, ImGuiWindowFlags.AlwaysAutoResize) != 0) {
                            ImGui.TextUnformatted("Do you want to saved the unsaved modifications to this scene first?\n");
                            ImGui.Separator();

                            const float buttonWidth = 100;

                            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(60, 0));
                            {
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - (buttonWidth * 3 + ImGui.GetStyle().ItemSpacing.X * 2)) / 2);

                                if (ImGui.Button("Save", new Vector2(buttonWidth, 0))) {
                                    EditorScene.SaveScene(_closingScene);
                                    EditorScene.CloseScene(_closingScene);
                                    _closingScene = null;

                                    ImGui.CloseCurrentPopup();
                                }

                                ImGui.SetItemDefaultFocus();
                                ImGui.SameLine();

                                if (ImGui.Button("Don't save", new Vector2(buttonWidth, 0))) {
                                    EditorScene.CloseScene(_closingScene!);
                                    _closingScene = null;

                                    ImGui.CloseCurrentPopup();
                                }

                                ImGui.SameLine();

                                if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0))) {
                                    _closingScene = null;
                                    ImGui.CloseCurrentPopup();
                                }
                            }
                            ImGui.PopStyleVar();

                            ImGui.EndPopup();
                        }
                    }
                } else {
                    EditorScene.CloseScene(_closingScene);
                    _closingScene = null;
                }
            }

            if ((ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Middle)) && RiptideGUI.IsMouseOverVoid()) {
                Selections.Clear();
            }

            ImGui.End();
        }

        if (ImGui.BeginDragDropTarget()) {
            var payload = ImGui.AcceptDragDropPayload(AssetBrowserWindow.AssetDragDropRidsID);

            if (payload.NativePtr != null) {
                foreach (var rid in Selections.EnumerateSelectedResourceIDs()) {
                    if (!EditorResourceDatabase.TryConvertResourceIDToPath(rid, out var path) || Path.GetExtension(path.AsSpan()) is not AssetFileExtensions.Scene) continue;

                    Console.WriteLine("Open Scene");

                    EditorScene.OpenScene(path);
                }
            }

            ImGui.EndDragDropTarget();
        }

        return open;
    }

    private void DrawSceneHierarchy(Scene scene) {
        if (scene.RootEntityCount == 0) return;

        ImGui.PushStyleColor(ImGuiCol.TableRowBg, 0xFF1F1F1F);
        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, 0xFF2F2F2F);
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 18);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, 4));

        var table = ImGui.BeginTable("Hierarchy Table", 1, ImGuiTableFlags.RowBg);

        if (table) {
            DrawEntities(scene.EnumerateRootEntities());

            ImGui.EndTable();
        }

        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(2);
    }

    private void DrawEntities(IEnumerable<Entity> enumerable) {
        if (!enumerable.Any()) return;

        int idx = 0;
        foreach (var entity in enumerable) {
            ImGui.PushID(idx++);
            DrawEntity_Underlying(entity, out _, out _);
            ImGui.PopID();
        }

        void DrawEntity_Underlying(Entity entity, out Vector2 itemRectMin, out Vector2 itemRectMax) {
            var drawlist = ImGui.GetWindowDrawList();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (Selections.IsSelected(entity)) {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, 0xFF4F4F4F);
            }

            bool expandTree = ImGui.TreeNodeEx(GetHierarchyDisplayName(entity), ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.SpanAvailWidth | (entity.ChildCount == 0 ? ImGuiTreeNodeFlags.Bullet : 0));
            itemRectMin = ImGui.GetItemRectMin(); itemRectMax = ImGui.GetItemRectMax();

            DoEntitySelectCheck(entity);

            if (ImGui.BeginPopupContextItem("Context Menu", ImGuiPopupFlags.MouseButtonRight)) {
                if (ImGui.MenuItem("Create Child")) {
                    _enumeratingScene.CreateEntity(entity);
                }

                ImGui.EndPopup();
            }

            if (expandTree) {
                const uint TreeLineColor = 0xFFCCCCCC;

                Vector2 verticalLineStart = new(itemRectMin.X + 11, itemRectMax.Y);
                Vector2 verticalLineEnd = verticalLineStart;

                int idx = 0;
                foreach (var child in entity.EnumerateChildren()) {
                    ImGui.PushID(idx++);

                    float HorizontalTreeLineSize = ImGui.GetStyle().IndentSpacing - 8;
                    DrawEntity_Underlying(child, out var childRectMin, out var childRectMax);
                    float midpoint = (childRectMin.Y + childRectMax.Y) / 2.0f;
                    drawlist.AddLine(new Vector2(verticalLineStart.X, midpoint), new Vector2(verticalLineStart.X + HorizontalTreeLineSize, midpoint), TreeLineColor);
                    verticalLineEnd.Y = midpoint;

                    ImGui.PopID();
                }

                drawlist.AddLine(verticalLineStart, verticalLineEnd, TreeLineColor);

                ImGui.TreePop();
            }
        }
    }

    private static void DoEntitySelectCheck(Entity entity) {
        if (!ImGui.IsItemToggledOpen() && ImGui.IsItemClicked()) {
            if (ImGui.GetIO().KeyCtrl) {
                if (!Selections.Remove(entity)) {
                    Selections.Add(entity);
                }
            } else {
                Selections.SelectSingle(entity);
            }
        }
    }

    private static string GetHierarchyDisplayName(Entity entity) => string.IsNullOrEmpty(entity.Name) ? "<Unnamed Entity>" : entity.Name;

    protected override void OnDispose(bool disposeManaged) { }

    [MenuBarCallback(MenuBarSection.View, "Editor/Hierarchy")]
    private static void SummonWindow() {
        EditorWindows.GetOrAddWindowInstance<HierarchyWindow>();
    }
}