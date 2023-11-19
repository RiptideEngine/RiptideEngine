namespace RiptideEditor;

[ComponentDrawer<Camera>]
internal sealed class CameraComponentDrawer : BaseComponentDrawer {
    public override void Render() {
        var camera = (Camera)TargetComponent;

        if (ImGui.BeginTable("Properties", 2, ImGuiTableFlags.Resizable)) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            DoClearColorField(camera);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            DoProjectionTypeField(camera);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            DoProjectionArgumentField(camera);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            DoViewPlaneField(camera);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            DoViewportField(camera);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            DoScissorRectField(camera);

            ImGui.EndTable();
        }
    }

    private static void DoClearColorField(Camera camera) {
        ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Clear Color");

        ImGui.TableNextColumn();

        var col = camera.ClearColor;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.ColorEdit4("##ClearColor", ref Unsafe.As<RiptideMathematics.Color, Vector4>(ref col))) {
            camera.ClearColor = col;
        }
    }
    private static void DoProjectionTypeField(Camera camera) {
        ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Projection Type");

        ImGui.TableNextColumn();

        ImGui.SetNextItemWidth(-1);
        var type = camera.ProjectionType;
        if (RiptideGUI.EnumCombo("##ProjectionType", ref type)) {
            camera.ProjectionType = type;
        }
    }
    private static void DoProjectionArgumentField(Camera camera) {
        ImGui.AlignTextToFramePadding();

        switch (camera.ProjectionType) {
            case CameraProjection.Perspective:
                ImGui.TextUnformatted("Field Of View");
                break;

            case CameraProjection.Orthographics:
                ImGui.TextUnformatted("Orthographic Size");
                break;
        }

        ImGui.TableNextColumn();

        float _float;
        switch (camera.ProjectionType) {
            case CameraProjection.Perspective:
                _float = camera.PerspectiveFOV * 180 / float.Pi;

                ImGui.SetNextItemWidth(-1);
                if (ImGui.SliderFloat("##PerspectiveFOV", ref _float, 0.001f, 179.99f, "%.4f", ImGuiSliderFlags.AlwaysClamp)) {
                    camera.PerspectiveFOV = _float / 180f * float.Pi;
                }
                break;

            case CameraProjection.Orthographics:
                _float = camera.OrthographicSize;

                ImGui.SetNextItemWidth(-1);
                if (ImGui.DragFloat("##OrthographicSize", ref _float, 0.01f, 0.001f, 100, "%.4f", ImGuiSliderFlags.Logarithmic)) {
                    camera.OrthographicSize = _float;
                }
                break;
        }
    }
    private static void DoViewPlaneField(Camera camera) {
        ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Planes");

        ImGui.TableNextColumn();

        if (ImGui.BeginTable("Plane Fields", 2)) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            float _float = camera.NearPlane;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##Near", ref _float, 0.01f, 0.001f, camera.FarPlane)) {
                camera.NearPlane = _float;
            }
            ImGui.SetItemTooltip("Near Plane");
            ImGui.TableNextColumn();

            _float = camera.FarPlane;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##Near", ref _float, camera.FarPlane + 0.001f, float.PositiveInfinity, camera.FarPlane)) {
                camera.FarPlane = _float;
            }
            ImGui.SetItemTooltip("Far Plane");

            ImGui.EndTable();
        }
    }
    private static void DoViewportField(Camera camera) {
        ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Viewport");

        ImGui.TableNextColumn();

        float _float;

        if (ImGui.BeginTable("Viewport Properties", 4, ImGuiTableFlags.SizingFixedFit)) {
            ImGui.TableSetupColumn("Column 1", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Column 2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Column 3", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Column 4", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("X");
            ImGui.TableSetColumnIndex(1);

            _float = camera.Viewport.Position.X;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##X", ref _float, 0.004f)) {
                var vp = camera.Viewport;
                vp.Position.X = _float;
                camera.Viewport = vp;
            }

            ImGui.TableSetColumnIndex(2);
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Y");

            ImGui.TableSetColumnIndex(3);

            _float = camera.Viewport.Position.Y;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##Y", ref _float, 0.004f)) {
                var vp = camera.Viewport;
                vp.Position.Y = _float;
                camera.Viewport = vp;
            }

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("W");
            ImGui.TableSetColumnIndex(1);

            _float = camera.Viewport.Size.X;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##W", ref _float, 0.004f)) {
                var vp = camera.Viewport;
                vp.Size.X = _float;
                camera.Viewport = vp;
            }

            ImGui.TableSetColumnIndex(2);
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("H");

            ImGui.TableSetColumnIndex(3);

            _float = camera.Viewport.Size.Y;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##H", ref _float, 0.004f)) {
                var vp = camera.Viewport;
                vp.Size.Y = _float;
                camera.Viewport = vp;
            }

            ImGui.EndTable();
        }
    }
    private static void DoScissorRectField(Camera camera) {
        float _float;
        ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Scissor Rect");

        ImGui.TableNextColumn();

        if (ImGui.BeginTable("ScissorRect Properties", 4, ImGuiTableFlags.SizingFixedFit)) {
            ImGui.TableSetupColumn("##Column 1", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("##Column 2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##Column 3", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("##Column 4", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Min X");
            ImGui.TableSetColumnIndex(1);

            _float = camera.ScissorRect.Min.X;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##MinX", ref _float, 0.002f, 0, 1)) {
                var vp = camera.ScissorRect;
                vp.Min.X = _float;
                camera.ScissorRect = vp;
            }

            ImGui.TableSetColumnIndex(2);
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Min Y");

            ImGui.TableSetColumnIndex(3);

            _float = camera.ScissorRect.Min.Y;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##MinY", ref _float, 0.002f, 0, 1)) {
                var vp = camera.ScissorRect;
                vp.Min.Y = _float;
                camera.ScissorRect = vp;
            }

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Max X");
            ImGui.TableSetColumnIndex(1);

            _float = camera.ScissorRect.Max.X;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##MaxX", ref _float, 0.002f, 0, 1)) {
                var vp = camera.ScissorRect;
                vp.Max.X = _float;
                camera.ScissorRect = vp;
            }

            ImGui.TableSetColumnIndex(2);
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Max Y");

            ImGui.TableSetColumnIndex(3);

            _float = camera.ScissorRect.Max.Y;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.DragFloat("##MaxY", ref _float, 0.002f, 0, 1)) {
                var vp = camera.ScissorRect;
                vp.Max.Y = _float;
                camera.ScissorRect = vp;
            }

            ImGui.EndTable();
        }
    }
}