namespace RiptideEditor.Windows;

public sealed unsafe partial class AssetBrowserWindow : EditorWindow {
    public const float IconSize = 96;
    private static readonly string BreadcrumbSeparatorPopupID = "Breadcrumb Separator";
    private static readonly Vector2 BreadcrumbSeparatorImageSize = new(15, 15);

    private static ReadOnlySpan<char> OutsideRightclickPopupID => "Right-click Popup";
    private static ReadOnlySpan<byte> SingleFileMoveDuplicationModal => "Duplicate asset detected##Duplication Action\0"u8;

    private string _breadcrumbSeparatorDisplayPath = string.Empty;

    private Texture2D _smallIcons, _assetIcons;

    private string _searchingString;

    private string[] _moveSources;
    private string _moveDestinationFolder;

    public AssetBrowserWindow() {
        _searchingString = string.Empty;
        _browsingPath = string.Empty;
        _creatingAssetDirectory = _creatingAssetFileName = string.Empty;

        _moveSources = Array.Empty<string>();
        _moveDestinationFolder = string.Empty;

        var context = EditorApplication.RenderingContext;
        var factory = context.Factory;

        var cmdList = factory.CreateCommandList();

        using (var image = Image.Load<Rgba32>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "textures", "asset_browser_small.png"))) {
            bool op = image.DangerousTryGetSinglePixelMemory(out var memory);
            Debug.Assert(op, "Failed to get single pixel memory from Image<Rgba32>.");

            _smallIcons = new Texture2D((ushort)image.Width, (ushort)image.Height, GraphicsFormat.R8G8B8A8UNorm);

            cmdList.UpdateResource(_smallIcons.UnderlyingTexture, MemoryMarshal.AsBytes(memory.Span));
            cmdList.TranslateResourceStates([
                new(_smallIcons.UnderlyingTexture, ResourceStates.CopyDestination, ResourceStates.ShaderResource),
            ]);
        }

        using (var image = Image.Load<Rgba32>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "textures", "asset_icons.png"))) {
            bool op = image.DangerousTryGetSinglePixelMemory(out var memory);
            Debug.Assert(op, "Failed to get single pixel memory from Image<Rgba32>.");

            _assetIcons = new((ushort)image.Width, (ushort)image.Height, GraphicsFormat.R8G8B8A8UNorm);

            cmdList.UpdateResource(_assetIcons.UnderlyingTexture, MemoryMarshal.AsBytes(memory.Span));
            cmdList.TranslateResourceStates([
                new(_assetIcons.UnderlyingTexture, ResourceStates.CopyDestination, ResourceStates.ShaderResource),
            ]);
        }

        cmdList.Close();
        context.ExecuteCommandList(cmdList);
        context.WaitForGpuIdle();

        cmdList.DecrementReference();
    }

    public override bool Render() {
        bool open = true;

        bool render = ImGui.Begin("Asset Browser", ref open, ImGuiWindowFlags.None);

        if (render) {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
            {
                DoNavigationButtons();

                ImGui.SameLine();

                var availWidth = ImGui.GetContentRegionAvail().X;
                var searchbarWidth = availWidth * 0.25f;

                DoBreadcrumb(availWidth - searchbarWidth - ImGui.GetStyle().ItemSpacing.X);

                ImGui.SameLine();
                ImGui.SetNextItemWidth(searchbarWidth);

                DoSearchBox();
            }
            ImGui.PopStyleVar();

            if (ImGui.BeginChild("Hierarch & Listing", new Vector2(-1), false, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)) {
                if (ImGui.BeginTable("Table", 2, ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable)) {
                    if (ImGui.TableNextColumn()) {
                        DoDirectoryHierarchy();
                    }

                    if (ImGui.TableNextColumn()) {
                        DoFileListing();
                    }

                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();

            if (_selectedBrowsingPath != null) {
                _browsingPath = _selectedBrowsingPath;
                _selectedBrowsingPath = null!;
            }

            if (_moveSources.Length != 0) {
                if (_moveSources.Length == 1) {
                    fixed (byte* pModalLabel = SingleFileMoveDuplicationModal) {
                        var dest = Path.Combine(_moveDestinationFolder, Path.GetFileName(_moveSources[0]));

                        if (File.Exists(dest)) {
                            ImGuiNative.igOpenPopup_Str(pModalLabel, ImGuiPopupFlags.None);
                        } else {
                            File.Move(_moveSources[0], dest);
                            _moveSources = [];
                        }

                        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

                        if (ImGuiNative.igBeginPopupModal(pModalLabel, null, ImGuiWindowFlags.AlwaysAutoResize) != 0) {
                            ImGui.TextUnformatted("Asset with the same name and extension is already exists in the destination folder.\nDo you want to replace it?");

                            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(40, ImGui.GetStyle().ItemSpacing.Y));

                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X / 2) - (300 + ImGui.GetStyle().ItemSpacing.X) / 2);

                            if (ImGui.Button("Replace", new Vector2(150, 0))) {
                                File.Delete(dest);
                                File.Move(_moveSources[0], dest);

                                _moveSources = Array.Empty<string>();
                            }

                            ImGui.SameLine();

                            if (ImGui.Button("Cancel", new Vector2(150, 0))) {
                                ImGui.CloseCurrentPopup();
                                _moveSources = Array.Empty<string>();
                            }

                            ImGui.PopStyleVar();

                            ImGui.EndPopup();
                        }
                    }
                }
            }

            ImGui.End();
        }

        return open;
    }

    private void DoSearchBox() {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 2));

        bool searchWindow = ImGui.BeginChild("Searchbox", new(ImGui.CalcItemWidth(), ImGui.GetFrameHeightWithSpacing()), true, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.HorizontalScrollbar);

        ImGui.PopStyleVar();

        if (searchWindow) {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, ImGui.GetStyle().ItemSpacing.Y));

            var height = ImGui.GetWindowContentRegionMax().Y - ImGui.GetWindowContentRegionMin().Y;

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - height - ImGui.GetStyle().ItemSpacing.X);

            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.InputText("##SearchInput", ref _searchingString, 128);
            ImGui.PopStyleColor();

            ImGui.SameLine();

            var padding = new Vector2(height / 2 - BreadcrumbSeparatorImageSize.X / 2, height / 2 - BreadcrumbSeparatorImageSize.Y / 2);

            ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF6F6F6F);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF4A4A4A);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, padding);

            if (string.IsNullOrEmpty(_searchingString)) {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + padding);
                ImGui.Image((nint)_smallIcons.UnderlyingView.NativeView.Handle, new Vector2(height) - padding * 2, new Vector2(0.75f, 0), new Vector2(1, 0.25f));
            } else {
                bool btn = ImGui.ImageButton("##Separator", (nint)_smallIcons.UnderlyingView.NativeView.Handle, BreadcrumbSeparatorImageSize, new Vector2(0f, 0.25f), new Vector2(0.25f, 0.5f));

                if (btn) {
                    _searchingString = string.Empty;
                }
            }

            ImGui.PopStyleVar();

            ImGui.PopStyleColor(3);

            ImGui.PopStyleVar();
        }

        ImGui.EndChild();
    }

    protected override void OnDispose(bool disposeManaged) {
        base.OnDispose(disposeManaged);

        _smallIcons.DecrementReference(); _smallIcons = null!;
        _assetIcons.DecrementReference(); _assetIcons = null!;
    }

    [MenuBarCallback(MenuBarSection.View, "Editor/Asset Browser")]
    private static void SummonWindow() {
        EditorWindows.GetOrAddWindowInstance<AssetBrowserWindow>();
    }
}