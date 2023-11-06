using System.Linq;

namespace RiptideEditor.Windows;

unsafe partial class AssetBrowserWindow {
    private static ReadOnlySpan<byte> AssetDeletionModal => "Delete asset?##File Delete Modal\0"u8;
    public static ReadOnlySpan<char> AssetDragDropRidsID => "RIPTIDE_DRAGGING_RESOURCE";

    private string _creatingAssetDirectory;
    private string _creatingAssetFileName;
    private Action<string, string>? _creatingAssetCallback;

    private IEnumerable<string>? _deletingFiles;

    private void DoFileListing() {
        var browsingFullPath = Path.Combine(EditorResourceDatabase.AssetDirectory, _browsingPath);

        if (ImGui.BeginPopupContextWindow("Void Popup Context", ImGuiPopupFlags.MouseButtonRight)) {
            if (ImGui.BeginMenu("Create")) {
                Span<Range> ranges = stackalloc Range[2];

                //foreach (var candidate in ResourceCreationContextMenuCollection.EnumerateCandidates()) {
                //    DoMenuRecursively(ranges, candidate.Attribute.MenuPath, candidate.Delegate);
                //}

                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Show in Explorer")) {
                EditorUtility.ShowInExplorer(Path.Combine(EditorResourceDatabase.AssetDirectory, _browsingPath));
            }

            ImGui.EndPopup();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));

        var availSize = ImGui.GetContentRegionAvail().X;
        var cellWidth = IconSize + ImGui.GetStyle().FramePadding.X * 2;

        int columnAmount = int.Max(1, (int)float.Floor(availSize / cellWidth));

        if (ImGui.BeginTable("Listing Table", columnAmount, ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX)) {
            byte* pColumnName = stackalloc byte[7 + 3 + 1] {
                (byte)'C', (byte)'o', (byte)'l', (byte)'u', (byte)'m', (byte)'n', (byte)' ',
                0, 0, 0, 0,
            };

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
            for (int i = 0; i < columnAmount; i++) {
                bool format = i.TryFormat(new Span<byte>(pColumnName + 7, 3), out int written);
                Debug.Assert(format);

                ImGuiNative.igTableSetupColumn(pColumnName, ImGuiTableColumnFlags.WidthFixed, cellWidth, 0);
            }
            ImGui.PopStyleVar();

            ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF6F6F6F);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF4A4A4A);

            ImGui.TableNextRow();

            ImGui.BeginGroup();

            // Enumerate directories
            foreach (var directory in Directory.EnumerateDirectories(browsingFullPath, _searchingString, SearchOption.TopDirectoryOnly)) {
                ImGui.TableNextColumn();

                ImGui.PushID(Path.GetFileName(directory.AsSpan()));

                var relativePath = EditorResourceDatabase.TruncateResourcePath(directory)!;

                DoDirectoryIconButton(relativePath, Vector2.Zero, new Vector2(0.0625f, 1f));
                DoFileContextMenu(relativePath);

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                    Navigate(Path.Join(_browsingPath, Path.GetFileName(directory.AsSpan())));

                    EditorResourceDatabase.TryConvertPathToResourceID(relativePath, out var rid);
                    Selections.Remove(rid);
                }

                ImGui.SetNextItemWidth(-1);

                DrawFileName(directory);

                ImGui.PopID();
            }

            // Enumerate files
            foreach (var file in EditorResourceDatabase.EnumerateAssetFileSystems(browsingFullPath, SearchOption.TopDirectoryOnly)) {
                ImGui.TableNextColumn();

                ReadOnlySpan<char> fileName = Path.GetFileName(file.AsSpan());

                ImGui.PushID(fileName);

                Vector2 uvMin = new(0.0625f, 0f), uvMax = new(0.125f, 1f);

                switch (Path.GetExtension(file.AsSpan())) {
                    case ".cs":
                        uvMin = new(0.125f, 0f);
                        uvMax = new(0.1875f, 1f);
                        break;

                    case ".png":
                        uvMin = new(0.1875f, 0f);
                        uvMax = new(0.25f, 1f);
                        break;

                    case ".ogg" or ".wav":
                        uvMin = new(0.25f, 0f);
                        uvMax = new(0.3125f, 1f);
                        break;

                    case ".txt":
                        uvMin = new(0.3125f, 0f);
                        uvMax = new(0.375f, 1f);
                        break;

                    case ".obj" or ".fbx" or ".gltf" or ".glb":
                        uvMin = new(0.375f, 0f);
                        uvMax = new(0.4375f, 1f);
                        break;

                    case ".gscene":
                        uvMin = new(0.4375f, 0f);
                        uvMax = new(0.5f, 1f);
                        break;
                }

                var relativePath = EditorResourceDatabase.TruncateResourcePath(file)!;
                DoFileIconButton(relativePath, uvMin, uvMax);

                DoFileContextMenu(relativePath);

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                    EditorResourceDatabase.TryConvertPathToResourceID(relativePath, out var rid);

                    AssetOpenInvoker.Invoke(relativePath);
                }

                ImGui.SetNextItemWidth(-1);

                DrawFileName(file);

                ImGui.PopID();
            }

            ImGui.EndGroup();

            if (RiptideGUI.IsAnyMouseClicked() && RiptideGUI.IsMouseOverVoid()) {
                Selections.Clear();
            }

            if (!string.IsNullOrEmpty(_creatingAssetDirectory)) {
                ImGui.TableNextColumn();

                ImGui.PushID("##File Creation");

                Vector2 uvMin = new(0.0625f, 0f), uvMax = new(0.125f, 1f);

                ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetStyle().FramePadding);
                ImGui.Image((nint)_assetIcons.UnderlyingView.NativeView.Handle, new Vector2(IconSize), uvMin, uvMax);

                ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
                ImGui.PushStyleColor(ImGuiCol.Border, 0xFF5F5F5F);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);

                ImGui.SetNextItemWidth(-1);

                if (ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive()) {
                    ImGui.SetKeyboardFocusHere(0);
                }

                if (ImGui.InputText("##Naming", ref _creatingAssetFileName, 128, ImGuiInputTextFlags.EnterReturnsTrue)) {
                    _creatingAssetCallback!.Invoke(_browsingPath, _creatingAssetFileName);
                    _creatingAssetDirectory = _creatingAssetFileName = string.Empty;
                    _creatingAssetCallback = null;
                }

                if (ImGui.IsItemDeactivated()) {
                    _creatingAssetDirectory = string.Empty;
                }

                ImGui.PopStyleVar();
                ImGui.PopStyleColor(2);

                ImGui.PopID();
            }

            ImGui.PopStyleColor(3);

            ImGui.EndTable();
        }

        ImGui.PopStyleVar();

        if (_deletingFiles != null) {
            fixed (byte* pName = AssetDeletionModal) {
                if (ImGuiNative.igIsPopupOpen_Str(pName, ImGuiPopupFlags.None) == 0) {
                    ImGuiNative.igOpenPopup_Str(pName, ImGuiPopupFlags.None);
                }

                if (ImGuiNative.igBeginPopupModal(pName, null, ImGuiWindowFlags.AlwaysAutoResize) != 0) {
                    ImGui.TextUnformatted("Do you want to delete these asset files?\nThis operation cannot be undone.\n\n(Because there is no API to move file into the Recycle Bin for some fucking reason).");

                    ImGui.Separator();

                    bool no = false;
                    ImGui.Checkbox("Do not ask me next time", ref no);

                    ImGui.SetItemTooltip("This shit actually doesn't work lmao");

                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(150, 0));
                    {
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - (120 * 2 + ImGui.GetStyle().ItemSpacing.X) / 2);

                        if (ImGui.Button("OK", new Vector2(120, 0))) {
                            foreach (var path in _deletingFiles) {
                                EditorResourceDatabase.DeleteAsset(path);
                            }

                            _deletingFiles = null;
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.SetItemDefaultFocus();
                        ImGui.SameLine();

                        if (ImGui.Button("Cancel", new Vector2(120, 0))) {
                            _deletingFiles = null;
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.PopStyleVar();

                    ImGui.EndPopup();
                }
            }

        }
    }

    private void DoMenuRecursively(Span<Range> ranges, ReadOnlySpan<char> remainPath, Action<string, string> callback) {
        int split = remainPath.Split(ranges, '/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (split == 1) {
            if (ImGui.MenuItem(remainPath[ranges[0]])) {
                _creatingAssetDirectory = Path.Combine(EditorResourceDatabase.AssetDirectory, _browsingPath);
                _creatingAssetFileName = string.Empty;
                _creatingAssetCallback = callback;
            }
            return;
        }

        if (ImGui.BeginMenu(remainPath[ranges[0]])) {
            DoMenuRecursively(ranges, remainPath[ranges[1]], callback);

            ImGui.EndMenu();
        }
    }

    private void DoIconDragSource(bool isSelected, Guid rid, ImGuiDragDropFlags flags) {
        if (ImGui.BeginDragDropSource(flags)) {
            int numSelected = Selections.NumSelectedGuids;
            var pool = ArrayPool<Guid>.Shared.Rent(numSelected);

            int idx = 0;
            foreach (var selectedRid in Selections.EnumerateSelectedResourceIDs()) {
                pool[idx++] = selectedRid;
            }

            fixed (Guid* pinned = pool) {
                ImGui.SetDragDropPayload(AssetDragDropRidsID, (nint)(&rid), (uint)sizeof(Guid));
            }

            ArrayPool<Guid>.Shared.Return(pool);

            ImGui.TextUnformatted($"Dragging...");

            ImGui.EndDragDropSource();
        }
    }
    private void DoFileIconButton(string relativePath, Vector2 uvMin, Vector2 uvMax) {
        EditorResourceDatabase.TryConvertPathToResourceID(relativePath, out var rid);
        bool selected = Selections.IsSelected(rid);
        uint buttonColor = selected ? 0xFF454545 : 0;

        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);

        if (ImGui.ImageButton("##IconButton", (nint)_assetIcons.UnderlyingView.NativeView.Handle, new Vector2(IconSize), uvMin, uvMax)) {
            if (ImGui.GetIO().KeyCtrl) {
                if (!Selections.Remove(rid)) {
                    Selections.Add(rid);
                }
            } else {
                Selections.SelectSingle(rid);
            }
        }

        ImGui.PopStyleColor();

        DoIconDragSource(selected, rid, ImGuiDragDropFlags.None);
    }
    private void DoDirectoryIconButton(string relativePath, Vector2 uvMin, Vector2 uvMax) {
        EditorResourceDatabase.TryConvertPathToResourceID(relativePath, out var rid);
        bool selected = Selections.IsSelected(rid);
        uint buttonColor = selected ? 0xFF454545 : 0;

        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);

        if (ImGui.ImageButton("##IconButton", (nint)_assetIcons.UnderlyingView.NativeView.Handle, new Vector2(IconSize), uvMin, uvMax)) {
            if (ImGui.GetIO().KeyCtrl) {
                if (!Selections.Remove(rid)) {
                    Selections.Add(rid);
                }
            } else {
                Selections.SelectSingle(rid);
            }
        }

        ImGui.PopStyleColor();

        DoIconDragSource(selected, rid, ImGuiDragDropFlags.None);

        if (!selected) {
            if (ImGui.BeginDragDropTarget()) {
                var payload = ImGui.AcceptDragDropPayload(AssetDragDropRidsID, ImGuiDragDropFlags.AcceptNoDrawDefaultRect);

                if (payload.NativePtr != null) {
                    var primaryDrag = Unsafe.Read<Guid>((void*)payload.Data);

                    foreach (var moveRid in Selections.IsSelected(primaryDrag) ? Selections.EnumerateSelectedResourceIDs() : Selections.EnumerateSelectedResourceIDs().Prepend(primaryDrag)) {
                        if (!EditorResourceDatabase.TryConvertResourceIDToPath(moveRid, out var path)) continue;

                        EditorResourceDatabase.MoveAsset(path!, relativePath);
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }
    }

    private void DoFileContextMenu(string relativePath) {
        if (ImGui.BeginPopupContextItem("Item Context Menu", ImGuiPopupFlags.MouseButtonRight)) {
            if (ImGui.MenuItem("Delete")) {
                bool get = EditorResourceDatabase.TryConvertPathToResourceID(relativePath, out var guid);
                Debug.Assert(get);

                if (Selections.IsSelected(guid)) {
                    _deletingFiles = Selections.EnumerateSelectedResourceIDs().Select(EditorResourceDatabase.ConvertResourceIDToPath).FilterNull();
                } else {
                    _deletingFiles = new string[] { relativePath };
                }
            }

            if (ImGui.MenuItem("Rename")) {
            }

            ImGui.SetItemTooltip("Currently work in progress");

            ImGui.EndPopup();
        }
    }
    private void DrawFileName(ReadOnlySpan<char> path) {
        var filename = Path.GetFileName(path);

        var textSize = ImGui.CalcTextSize(filename);
        var itemWidth = ImGui.CalcItemWidth();

        if (textSize.X >= itemWidth) {
            ImGui.SetItemTooltip(filename);

            var areaBegin = ImGui.GetWindowPos() + ImGui.GetCursorPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
            var areaEnd = areaBegin + new Vector2(itemWidth, textSize.Y);
            ImGuiAddition.RenderTextEllipsis(ImGui.GetWindowDrawList(), areaBegin, areaEnd, areaEnd.X, areaEnd.X, filename, ref textSize);

            ImGui.Dummy(areaEnd - areaBegin);
        } else {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + itemWidth / 2 - textSize.X / 2);
            ImGui.TextUnformatted(filename);
        }
    }

    private void CancelFileCreation() {
        _creatingAssetDirectory = _creatingAssetFileName = string.Empty;
        _creatingAssetCallback = null;
    }
}