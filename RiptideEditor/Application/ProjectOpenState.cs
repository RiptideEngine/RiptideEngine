namespace RiptideEditor.Application;

internal sealed partial class ProjectOpenState : ApplicationState {
    private const float ProjectTemplateIconSize = 64;
    private const uint ProjectTemplateButtonHash = 0xCFFF93EE;

    private Texture2D _projectTemplateIcon = null!;

    private ProjectCreator? _projectCreator;

    public override void Begin() {
        using (var image = Image.Load<Rgba32>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Textures", "ProjectTemplateIcons.png"))) {
            bool get = image.DangerousTryGetSinglePixelMemory(out var memory);
            Debug.Assert(get);
            
            _projectTemplateIcon = new((ushort)image.Width, (ushort)image.Height, GraphicsFormat.R8G8B8A8UNorm);

            var renderingCtx = RuntimeFoundation.RenderingService.Context;
            var cmdList = renderingCtx.Factory.CreateCommandList();

            cmdList.TranslateResourceStates([
                new(_projectTemplateIcon.UnderlyingTexture, ResourceStates.Common, ResourceStates.CopyDestination),
            ]);
            cmdList.UpdateResource(_projectTemplateIcon.UnderlyingTexture, MemoryMarshal.AsBytes(memory.Span));
            cmdList.TranslateResourceStates([
                new(_projectTemplateIcon.UnderlyingTexture, ResourceStates.CopyDestination, ResourceStates.ShaderResource),
            ]);

            cmdList.Close();
            renderingCtx.ExecuteCommandList(cmdList);
            renderingCtx.WaitForGpuIdle();
            cmdList.DecrementReference();
        }
    }

    public override void RenderGUI() {
        var viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.Begin("Project Browsing", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
        ImGui.PopStyleVar();

        ImGui.BeginTable("Table", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.PushFont(EditorApplication.ImguiController.FontTitle1);
            ImGui.Text("Recent Projects");
            ImGui.PopFont();

            ImGui.Text("Thonker.");

            ImGui.TableNextColumn();

            ImGui.PushFont(EditorApplication.ImguiController.FontTitle1);
            ImGui.Text("Getting Started");
            ImGui.PopFont();

            ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF6F6F6F);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF4A4A4A);

            ImGui.PushItemWidth(-1);

            ImGui.PushID(0);
            DrawProjectTemplateButton("2D Project", "Built-in Template.", (nint)_projectTemplateIcon.UnderlyingView.NativeView.Handle, Vector2.Zero, new(1f / 3f, 1f));
            ImGui.PopID();

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered()) {
                _projectCreator = new TwoDimensionalProjectCreator();
            }

            ImGui.PushID(1);
            DrawProjectTemplateButton("3D Project", "Built-in Template.", (nint)_projectTemplateIcon.UnderlyingView.NativeView.Handle, new(1f / 3f, 0f), new(2f / 3f, 1f));
            ImGui.PopID();

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered()) {
                _projectCreator = new ThreeDimensionalProjectCreator();
            }

            ImGui.PopItemWidth();

            ImGui.PopStyleColor(3);
        }
        ImGui.EndTable();

        if (!DoCreatorConfigurationModal()) {
            _projectCreator = null;
        }

        ImGui.End();
        return;

        static void DrawProjectTemplateButton(string name, string description, nint textureID, Vector2 uv0, Vector2 uv1) {
            var framePadding = ImGui.GetStyle().FramePadding;
            var itemSpacing = ImGui.GetStyle().ItemSpacing;

            var drawList = ImGui.GetWindowDrawList();
            var itemWidth = ImGui.CalcItemWidth();

            var nameSize = ImGui.CalcTextSize(name);

            var descriptionSize = ImGui.CalcTextSize(description, itemWidth - framePadding.X * 2 - itemSpacing.X - ProjectTemplateIconSize);

            var textHeight = nameSize.Y + itemSpacing.Y + descriptionSize.Y;
            var itemHeight = Math.Max(textHeight, ProjectTemplateIconSize) + framePadding.Y * 2;

            ImGui.InvisibleButton("Button", new(itemWidth, itemHeight));

            if (!ImGui.IsItemVisible()) return;

            var bb = new Bound2D(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            drawList.AddRectFilled(bb.Min, bb.Max, ImGui.GetColorU32(ImGui.IsItemActive() && ImGui.IsItemHovered() ? ImGuiCol.ButtonActive : ImGui.IsItemHovered() ? ImGuiCol.ButtonHovered : ImGuiCol.Button));

            var innerbb = new Bound2D(bb.Min + framePadding, bb.Max - framePadding);

            var iconbb = new Bound2D(innerbb.Min, innerbb.Min + new Vector2(ProjectTemplateIconSize));
            drawList.AddImage(textureID, iconbb.Min, iconbb.Max, uv0, uv1);

            var textColor = ImGui.GetColorU32(ImGuiCol.Text);

            var namePosition = iconbb.Min + new Vector2(ProjectTemplateIconSize + itemSpacing.X, 0);
            drawList.AddText(namePosition, textColor, name);

            ImGui.PushTextWrapPos(innerbb.Max.X);
            drawList.AddText(namePosition, textColor, name);
            ImGuiInternal.RenderTextWrapped(namePosition + nameSize with { X = 0 } + itemSpacing with { X = 0 }, description, innerbb.Max.X - namePosition.X);
            ImGui.PopTextWrapPos();
        }
    }

    public override void Update() {
    }

    private string _createProjectLocation = string.Empty;

    private bool DoCreatorConfigurationModal() {
        if (_projectCreator != null && !ImGui.IsPopupOpen("Project Creator Configuration")) {
            ImGui.OpenPopup("Project Creator Configuration");
        }

        ImGui.SetNextWindowSize(new Vector2(ImGui.GetMainViewport().WorkSize.X / 2.5f, 0));
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        
        bool popupOpen = true;
        if (!ImGui.BeginPopupModal("Project Creator Configuration", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize)) return popupOpen;
        
        ImGui.BeginTable("Table", 2);
        {
            ImGui.TableSetupColumn(null, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(null, ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Location");

            ImGui.TableNextColumn();

            {
                float browseTextWidth = ImGui.CalcTextSize("Browse").X;
                
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - browseTextWidth - ImGui.GetStyle().ItemSpacing.X - ImGui.GetStyle().FramePadding.X * 2);
                ImGui.InputText("##Location", ref _createProjectLocation, 255);

                ImGui.SameLine();

                if (ImGui.Button("Browse")) {
                    
                }
            }
            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Name");

            ImGui.TableNextColumn();

            string text = string.Empty;

            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##Name", ref text, 0);

            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Description");

            ImGui.TableNextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextMultiline("##Project Description", ref text, 0, new Vector2(ImGui.CalcItemWidth(), 60));

            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Company");

            ImGui.TableNextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##Company", ref text, 0);
        }
        ImGui.EndTable();

        // Buttons
        {
            ImGui.Spacing();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(40, ImGui.GetStyle().ItemSpacing.Y));
            var windowContentRegionWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
            var buttonWidth = windowContentRegionWidth / 2 * 0.65f;

            var size = buttonWidth * 2 + ImGui.GetStyle().ItemSpacing.X;

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (windowContentRegionWidth - size) / 2);

            if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0))) {
                ImGui.CloseCurrentPopup();
                _projectCreator = null!;
            }

            ImGui.SameLine();
            if (ImGui.Button("Create", new Vector2(buttonWidth, 0))) {
                Debug.Assert(_projectCreator != null);

                var state = EditorApplication.SwitchState<ProjectCreationState>();
                state.Creator = _projectCreator;
            }

            ImGui.PopStyleVar();
        }

        ImGui.EndPopup();

        return popupOpen;
    }

    private void DisposeResources() {
        _projectTemplateIcon.DecrementReference(); _projectTemplateIcon = null!;
    }

    public override void End() {
        DisposeResources();
    }

    public override void Shutdown() {
        DisposeResources();
    }
}