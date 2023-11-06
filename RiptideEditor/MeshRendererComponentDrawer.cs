namespace RiptideEditor;

[ComponentDrawer<MeshRenderer>]
internal sealed unsafe class MeshRendererComponentDrawer : BaseComponentDrawer {
    public override void Render() {
        var renderer = (MeshRenderer)TargetComponent;

        if (ImGui.BeginTable("Properties", 2, ImGuiTableFlags.Resizable)) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Mesh");

            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding(); ImGui.SetNextItemWidth(-1);
            ImGui.Dummy(new Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeightWithSpacing()));

            if (ImGui.BeginDragDropTarget()) {
                var payload = ImGui.AcceptDragDropPayload(AssetBrowserWindow.AssetDragDropRidsID, ImGuiDragDropFlags.AcceptBeforeDelivery);

                if (payload.NativePtr != null) {
                    Guid meshGuid = default;

                    if (payload.IsPreview()) {
                        var primaryGuid = Unsafe.Read<Guid>((void*)payload.Data);

                        foreach (var rid in Selections.IsSelected(primaryGuid) ? Selections.EnumerateSelectedResourceIDs() : Selections.EnumerateSelectedResourceIDs().Prepend(primaryGuid)) {
                            if (!EditorResourceDatabase.TryConvertResourceIDToPath(rid, out var path)) continue;
                            if (Path.GetExtension(path.AsSpan()) is not ".obj") continue;

                            meshGuid = rid;
                            break;
                        }
                    }

                    if (payload.IsDelivery()) {
                        if (meshGuid != default) {
                            var importResult = EditorResourceDatabase.LoadResource<Mesh>(meshGuid);

                            Console.WriteLine("Result: " + importResult.Error);

                            if (!importResult.HasError) {
                                Debug.Assert(importResult.Result is Mesh);
                                renderer.Mesh = (Mesh)importResult.Result!;

                                Console.WriteLine("Assign Mesh");
                            }
                        }
                    }
                }

                ImGui.EndDragDropTarget();
            }

            ImGui.EndTable();
        }
    }
}
