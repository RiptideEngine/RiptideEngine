namespace RiptideEditor.Windows;

partial class AssetBrowserWindow {
    private void DoDirectoryHierarchy() {
        ImGui.PushStyleColor(ImGuiCol.TableRowBg, 0xFF3F3F3F);
        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, 0xFF5C5C5C);

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
        if (ImGui.BeginTable("Hierarchy Table", 1, ImGuiTableFlags.RowBg)) {
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 18);

            DrawDirectoryHierarchy();

            ImGui.PopStyleVar();

            ImGui.EndTable();
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);

        void DrawDirectoryHierarchy() {
            DrawDirectoryHierarchy_Underlying(EditorResourceDatabase.AssetDirectory, string.Empty, "Assets", out _, out _);
        }

        void DrawDirectoryHierarchy_Underlying(string directoryPath, string relativeDirectoryPath, ReadOnlySpan<char> nodeDisplayName, out Vector2 itemRectMin, out Vector2 itemRectMax) {
            var drawList = ImGui.GetWindowDrawList();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var directoryEnumerable = Directory.EnumerateDirectories(Path.Combine(EditorResourceDatabase.AssetDirectory, relativeDirectoryPath));

            bool expandTree = ImGui.TreeNodeEx(nodeDisplayName, ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.SpanAvailWidth | (!directoryEnumerable.Any() ? ImGuiTreeNodeFlags.Bullet : 0));
            itemRectMin = ImGui.GetItemRectMin(); itemRectMax = ImGui.GetItemRectMax();

            if (!ImGui.IsItemToggledOpen() && ImGui.IsItemClicked(ImGuiMouseButton.Left) && !relativeDirectoryPath.Equals(EditorResourceDatabase.AssetDirectory)) {
                if (_browsingPath != relativeDirectoryPath) _selectedBrowsingPath = relativeDirectoryPath;
            }

            if (expandTree) {
                const uint TreeLineColor = 0xFFCCCCCC;

                Vector2 verticalLineStart = new Vector2(itemRectMin.X + 11, itemRectMax.Y);
                Vector2 verticalLineEnd = verticalLineStart;

                foreach (var dir in directoryEnumerable) {
                    var filename = Path.GetFileName(dir.AsSpan());

                    float HorizontalTreeLineSize = ImGui.GetStyle().IndentSpacing - 8;
                    DrawDirectoryHierarchy_Underlying(dir, Path.Combine(relativeDirectoryPath, filename.ToString()), filename, out var childRectMin, out var childRectMax);
                    float midpoint = (childRectMin.Y + childRectMax.Y) / 2.0f;
                    drawList.AddLine(new Vector2(verticalLineStart.X, midpoint), new Vector2(verticalLineStart.X + HorizontalTreeLineSize, midpoint), TreeLineColor);
                    verticalLineEnd.Y = midpoint;
                }

                drawList.AddLine(verticalLineStart, verticalLineEnd, TreeLineColor);

                ImGui.TreePop();
            }
        }
    }
}