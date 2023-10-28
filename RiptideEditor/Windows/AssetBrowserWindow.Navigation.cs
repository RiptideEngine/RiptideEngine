namespace RiptideEditor.Windows;

unsafe partial class AssetBrowserWindow {
    private const int NavigationStackMaximumCount = 64;

    private string _browsingPath;
    private string? _selectedBrowsingPath;
    private readonly Stack<string> _navigationUndo = new(), _navigationRedo = new();

    private void DoNavigationButtons() {
        ImGui.PushID("NavigationButtons");

        ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF8F8F8F);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF4F4F4F);

        ImGui.BeginDisabled(_navigationUndo.Count == 0);
        if (ImGui.ImageButton("##UndoNavigation", (nint)_smallIcons.ViewHandle.Handle, new Vector2(18, 18), Vector2.Zero, new Vector2(0.25f, 0.25f))) {
            UndoNavigation();
        }
        ImGui.EndDisabled();

        ImGui.SameLine(0, 8);

        ImGui.BeginDisabled(_navigationRedo.Count == 0);
        if (ImGui.ImageButton("##RedoNavigation", (nint)_smallIcons.ViewHandle.Handle, new Vector2(18, 18), new(0.25f, 0), new Vector2(0f, 0.25f))) {
            RedoNavigation();
        }
        ImGui.EndDisabled();

        ImGui.SameLine(0, 8);

        ImGui.BeginDisabled(_browsingPath.Length == 0);
        if (ImGui.ImageButton("##Backward", (nint)_smallIcons.ViewHandle.Handle, new Vector2(18, 18), new Vector2(0.25f, 0f), new Vector2(0.5f, 0.25f))) {
            NavigateUpward();
        }
        ImGui.EndDisabled();

        ImGui.PopStyleColor(3);

        ImGui.PopID();
    }

    public void UndoNavigation() {
        CancelFileCreation();

        if (_navigationUndo.TryPop(out var pop)) {
            _selectedBrowsingPath = pop;
            _navigationRedo.Push(pop);
        }
    }

    public void Navigate(string path) {
        CancelFileCreation();

        _navigationUndo.Push(_browsingPath);
        _selectedBrowsingPath = path;
        _navigationRedo.Clear();
    }

    public void RedoNavigation() {
        CancelFileCreation();

        if (_navigationRedo.TryPop(out var pop)) {
            _selectedBrowsingPath = pop;
            _navigationUndo.Push(pop);
        }
    }

    public void NavigateUpward() {
        CancelFileCreation();

        _selectedBrowsingPath = Path.GetDirectoryName(_browsingPath) ?? string.Empty;
        _navigationRedo.Clear();
    }

    private void DoBreadcrumb(float width) {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        bool childWindow = ImGui.BeginChild("Breadcrumb Navigation", new(width, ImGui.GetFrameHeightWithSpacing()), true, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar);

        ImGui.PopStyleVar();

        if (childWindow) {
            ImGui.SetScrollY(0);

            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF6F6F6F);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF4A4A4A);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

            DoBreadcrumbDirectoryButton("Assets", true, 0);

            ImGui.SameLine();

            if (DoBreadcrumbSeparator()) {
                _breadcrumbSeparatorDisplayPath = string.Empty;
                ImGui.OpenPopup(BreadcrumbSeparatorPopupID);
            }

            // Draw breadcrumb elements of addition paths.
            if (_browsingPath.Length > 0) {
                ImGui.SameLine();

                Span<Range> ranges = stackalloc Range[2];
                DrawBreadcrumbElementRecursive(ranges, _browsingPath, 0, 0);
            }

            ImGui.PopStyleVar();

            ImGui.PopStyleColor(2);

            DoSeparatorPopup();
        }
        ImGui.EndChild();

        void DoBreadcrumbDirectoryButton(ReadOnlySpan<char> text, bool dropTarget, int pathCutoff) {
            var textSize = ImGui.CalcTextSize(text);
            var btnWidth = textSize.X + ImGui.GetStyle().FramePadding.X * 2;

            var cp = ImGui.GetCursorPos();

            ImGui.Dummy(new Vector2(btnWidth, ImGui.GetWindowHeight()));

            uint btnColor = 0;

            if (dropTarget) {
                DoBreadcrumbDropTarget(ref btnColor, pathCutoff);
            }

            ImGui.SetCursorPos(cp);

            ImGui.PushStyleColor(ImGuiCol.Button, btnColor);

            if (ImGui.Button(text, new(btnWidth, ImGui.GetWindowHeight()))) {
                _browsingPath = pathCutoff == 0 ? string.Empty : _browsingPath[..pathCutoff];
            }

            ImGui.PopStyleColor();
        }

        bool DoBreadcrumbSeparator() {
            var height = ImGui.GetWindowHeight();
            var padding = new Vector2((height - BreadcrumbSeparatorImageSize.X) / 2, (height - BreadcrumbSeparatorImageSize.Y) / 2);

            ImGui.PushStyleColor(ImGuiCol.Button, 0);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, padding);
            bool btn = ImGui.ImageButton("##Separator", (nint)_smallIcons.ViewHandle.Handle, BreadcrumbSeparatorImageSize, new Vector2(0.5f, 0), new Vector2(0.75f, 0.25f));
            ImGui.PopStyleVar();

            ImGui.PopStyleColor();

            return btn;
        }

        void DoBreadcrumbDropTarget(ref uint buttonColor, int pathCutoff) {
            if (ImGui.BeginDragDropTarget()) {
                var payload = ImGui.AcceptDragDropPayload(AssetDragDropRidsID, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);

                if (payload.NativePtr != null) {
                    if (payload.IsPreview()) {
                        buttonColor = 0xFF3F3F3F;
                    }

                    if (payload.IsDelivery()) {
                        string dest = pathCutoff == 0 ? string.Empty : _browsingPath[..pathCutoff];
                        var primaryGuid = Unsafe.Read<Guid>((void*)payload.Data);

                        foreach (var rid in Selections.IsSelected(primaryGuid) ? Selections.EnumerateSelectedResourceIDs() : Selections.EnumerateSelectedResourceIDs().Prepend(primaryGuid)) {
                            EditorResourceDatabase.TryConvertResourceIDToPath(rid, out var path);

                            EditorResourceDatabase.MoveAsset(path!, dest);
                        }
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        void DrawBreadcrumbElementRecursive(Span<Range> ranges, ReadOnlySpan<char> currentPath, int depth, int pathCutoff) {
            int split = currentPath.Split(ranges, Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            pathCutoff += ranges[0].End.GetOffset(currentPath.Length) + (split - 1);
            ImGui.PushID(depth);

            DoBreadcrumbDirectoryButton(currentPath[ranges[0]], split == 2, pathCutoff);

            ImGui.SameLine();

            if (DoBreadcrumbSeparator()) {
                _breadcrumbSeparatorDisplayPath = _browsingPath[..pathCutoff];

                ImGui.OpenPopup(BreadcrumbSeparatorPopupID);
            }

            DoSeparatorPopup();

            if (split == 2) {
                ImGui.SameLine();

                DrawBreadcrumbElementRecursive(ranges, currentPath[ranges[1]], depth + 1, pathCutoff);
            }

            ImGui.PopID();
        }

        void DoSeparatorPopup() {
            if (ImGui.BeginPopup(BreadcrumbSeparatorPopupID)) {
                Debug.Assert(_breadcrumbSeparatorDisplayPath != null);

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 4));

                foreach (var dir in Directory.EnumerateDirectories(Path.Combine(EditorResourceDatabase.AssetDirectory, _breadcrumbSeparatorDisplayPath))) {
                    if (ImGui.Selectable(Path.GetFileName(dir.AsSpan()))) {
                        _selectedBrowsingPath = Path.Combine(_breadcrumbSeparatorDisplayPath, Path.GetFileName(dir.AsSpan()).ToString());
                    }
                }
                ImGui.PopStyleVar(2);

                ImGui.EndPopup();
            }
        }
    }
}