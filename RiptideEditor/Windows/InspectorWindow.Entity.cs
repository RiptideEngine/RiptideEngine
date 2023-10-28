namespace RiptideEditor.Windows;

partial class InspectorWindow {
    private void DoEntityInspecting() {
        DoEntityEditor();

        DoComponentDrawing();

        ImGui.Spacing();

        var btnWidth = ImGui.GetContentRegionAvail().X * 0.75f;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - btnWidth) / 2);

        if (ImGui.Button("Add Component", new Vector2(btnWidth, 30))) {
            ImGui.OpenPopup("Add Component Popup");
        }

        if (ImGui.BeginPopup("Add Component Popup")) {
            foreach (var assembly in EditorApplication.Services.GetRequiredService<IComponentDatabase>().EnumerateComponentTypes().GroupBy(x => x.Value.Assembly)) {
                if (ImGui.BeginMenu(assembly.Key.GetName().Name)) {
                    foreach ((_, var componentType) in assembly) {
                        if (ImGui.MenuItem(componentType.Name)) {
                            if (Selections.NumSelectedEntities == 1) {
                                var c = Selections.EnumerateSelectedEntities().First().AddComponent(componentType);
                                AddComponentSingleEntityDrawer(c);
                            }
                        }
                    }
                }
            }

            ImGui.EndPopup();
        }
    }

    private static void DoEntityEditor() {
        if (ImGui.CollapsingHeader("Entity##__RIPTIDE_ENTITY_INSPECTOR__")) {
            var entityEnumerable = Selections.EnumerateSelectedEntities();
            var firstEntity = entityEnumerable.First();

            if (ImGui.BeginTable("Table", 2, ImGuiTableFlags.Resizable)) {
                DoNameField(firstEntity, entityEnumerable);
                DoTransformationFields(firstEntity, entityEnumerable);

                ImGui.EndTable();
            }
        }

        static void DoNameField(Entity firstSelectedEntity, IEnumerable<Entity> entityEnumerable) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Name");

            ImGui.TableNextColumn();

            if (entityEnumerable.Count() == 1) {
                var name = firstSelectedEntity.Name;
                ImGui.PushItemWidth(-1);
                if (ImGui.InputText("##Name", ref name, 64, ImGuiInputTextFlags.EnterReturnsTrue)) {
                    firstSelectedEntity.Name = name;
                }
            } else {
                var firstName = firstSelectedEntity.Name;
                bool different = false;

                foreach (var entity in entityEnumerable.Skip(1)) {
                    if (entity.Name != firstName) {
                        different = true;
                        break;
                    }
                }

                ImGui.PushItemWidth(-1);

                var displayName = different ? "-" : firstName;
                if (ImGui.InputText("##Name", ref displayName, 64, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.EscapeClearsAll)) {
                    Console.WriteLine("Set");

                    foreach (var entity in entityEnumerable) {
                        entity.Name = displayName;
                    }
                }
            }
        }
        static void DoTransformationFields(Entity firstSelectedEntity, IEnumerable<Entity> entityEnumerable) {
            bool singleEntity = entityEnumerable.Count() == 1;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Position");

            ImGui.TableNextColumn();

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2, 0));
            if (ImGui.BeginTable("Position", 6, ImGuiTableFlags.SizingFixedFit)) {
                ImGui.TableSetupColumn("##Column 1", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Column 3", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 4", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Column 5", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 6", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("X");
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Y");
                ImGui.TableSetColumnIndex(4);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Z");

                if (singleEntity) {
                    Vector3 position = firstSelectedEntity.LocalPosition;

                    ImGui.TableSetColumnIndex(1); ImGui.SetNextItemWidth(-1);
                    bool changed = ImGui.DragFloat("##X", ref position.X, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    ImGui.TableSetColumnIndex(3); ImGui.SetNextItemWidth(-1);
                    changed |= ImGui.DragFloat("##Y", ref position.Y, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    ImGui.TableSetColumnIndex(5); ImGui.SetNextItemWidth(-1);
                    changed |= ImGui.DragFloat("##Z", ref position.Z, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    if (changed) {
                        firstSelectedEntity.LocalPosition = position;
                    }
                } else {
                    float value = firstSelectedEntity.LocalPosition.X;
                    var distinct = entityEnumerable.Select(LocalPositionXGetter).Distinct();
                    ImGui.TableSetColumnIndex(1); ImGui.PushItemWidth(-1);
                    if (ImGui.DragFloat("##X", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                        foreach (var entity in entityEnumerable) {
                            entity.LocalPosition = entity.LocalPosition with { X = value };
                        }
                    }

                    value = firstSelectedEntity.LocalPosition.Y;
                    distinct = entityEnumerable.Select(LocalPositionYGetter).Distinct();
                    ImGui.TableSetColumnIndex(3); ImGui.PushItemWidth(-1);
                    if (ImGui.DragFloat("##Y", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                        foreach (var entity in entityEnumerable) {
                            entity.LocalPosition = entity.LocalPosition with { Y = value };
                        }
                    }

                    value = firstSelectedEntity.LocalPosition.Z;
                    distinct = entityEnumerable.Select(LocalPositionZGetter).Distinct();
                    ImGui.TableSetColumnIndex(5); ImGui.PushItemWidth(-1);
                    if (ImGui.DragFloat("##Z", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                        foreach (var entity in entityEnumerable) {
                            entity.LocalPosition = entity.LocalPosition with { Z = value };
                        }
                    }
                }

                ImGui.EndTable();
            }
            ImGui.PopStyleVar();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Rotation");

            ImGui.TableNextColumn();

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2, 0));
            if (ImGui.BeginTable("Rotation", 6, ImGuiTableFlags.SizingFixedFit)) {
                ImGui.TableSetupColumn("##Column 1", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Column 3", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 4", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Column 5", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 6", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("X");
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Y");
                ImGui.TableSetColumnIndex(4);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Z");

                if (singleEntity) {
                    firstSelectedEntity.GlobalRotation.ExtractYawPitchRoll(out var yaw, out var pitch, out var roll);
                    var euler = new Vector3(pitch, yaw, roll) / float.Pi * 180;

                    ImGui.TableSetColumnIndex(1); ImGui.SetNextItemWidth(-1);
                    bool changed = ImGui.DragFloat("##X", ref euler.X, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    ImGui.TableSetColumnIndex(3); ImGui.SetNextItemWidth(-1);
                    changed |= ImGui.DragFloat("##Y", ref euler.Y, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    ImGui.TableSetColumnIndex(5); ImGui.SetNextItemWidth(-1);
                    changed |= ImGui.DragFloat("##Z", ref euler.Z, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    if (changed) {
                        euler *= float.Pi / 180f;
                        firstSelectedEntity.LocalRotation = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
                    }
                } else {
                    //float value = firstSelectedEntity.LocalPosition.X;
                    //var distinct = entityEnumerable.Select(x => x.LocalPosition.X).Distinct();
                    //ImGui.TableSetColumnIndex(1); ImGui.PushItemWidth(-1);
                    //if (ImGui.DragFloat("##X", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                    //    foreach (var entity in entityEnumerable) {
                    //        entity.LocalPosition = entity.LocalPosition with { X = value };
                    //    }
                    //}

                    //value = firstSelectedEntity.LocalPosition.Y;
                    //distinct = entityEnumerable.Select(x => x.LocalPosition.Y).Distinct();
                    //ImGui.TableSetColumnIndex(3); ImGui.PushItemWidth(-1);
                    //if (ImGui.DragFloat("##Y", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                    //    foreach (var entity in entityEnumerable) {
                    //        entity.LocalPosition = entity.LocalPosition with { Y = value };
                    //    }
                    //}

                    //value = firstSelectedEntity.LocalPosition.Z;
                    //distinct = entityEnumerable.Select(x => x.LocalPosition.Z).Distinct();
                    //ImGui.TableSetColumnIndex(5); ImGui.PushItemWidth(-1);
                    //if (ImGui.DragFloat("##Z", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                    //    foreach (var entity in entityEnumerable) {
                    //        entity.LocalPosition = entity.LocalPosition with { Z = value };
                    //    }
                    //}
                }

                ImGui.EndTable();
            }
            ImGui.PopStyleVar();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Scale");

            ImGui.TableNextColumn();

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2, 0));
            if (ImGui.BeginTable("Scale", 6, ImGuiTableFlags.SizingFixedFit)) {
                ImGui.TableSetupColumn("##Column 1", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 2", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Column 3", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 4", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Column 5", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Column 6", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("X");
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Y");
                ImGui.TableSetColumnIndex(4);
                ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted("Z");

                if (singleEntity) {
                    Vector3 scale = firstSelectedEntity.LocalScale;

                    ImGui.TableSetColumnIndex(1); ImGui.SetNextItemWidth(-1);
                    bool changed = ImGui.DragFloat("##X", ref scale.X, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    ImGui.TableSetColumnIndex(3); ImGui.SetNextItemWidth(-1);
                    changed |= ImGui.DragFloat("##Y", ref scale.Y, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    ImGui.TableSetColumnIndex(5); ImGui.SetNextItemWidth(-1);
                    changed |= ImGui.DragFloat("##Z", ref scale.Z, 0.01f, float.NegativeInfinity, float.PositiveInfinity);

                    if (changed) {
                        firstSelectedEntity.LocalScale = scale;
                    }
                } else {
                    float value = firstSelectedEntity.LocalScale.X;
                    var distinct = entityEnumerable.Select(LocalScaleXGetter).Distinct();
                    ImGui.TableSetColumnIndex(1); ImGui.PushItemWidth(-1);
                    if (ImGui.DragFloat("##X", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                        foreach (var entity in entityEnumerable) {
                            entity.LocalScale = entity.LocalScale with { X = value };
                        }
                    }

                    value = firstSelectedEntity.LocalScale.Y;
                    distinct = entityEnumerable.Select(LocalScaleYGetter).Distinct();
                    ImGui.TableSetColumnIndex(3); ImGui.PushItemWidth(-1);
                    if (ImGui.DragFloat("##Y", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                        foreach (var entity in entityEnumerable) {
                            entity.LocalScale = entity.LocalScale with { Y = value };
                        }
                    }

                    value = firstSelectedEntity.LocalScale.Z;
                    distinct = entityEnumerable.Select(LocalScaleZGetter).Distinct();
                    ImGui.TableSetColumnIndex(5); ImGui.PushItemWidth(-1);
                    if (ImGui.DragFloat("##Z", ref value, 0.01f, float.NegativeInfinity, float.PositiveInfinity, distinct.Count() > 1 ? "-" : ReadOnlySpan<char>.Empty)) {
                        foreach (var entity in entityEnumerable) {
                            entity.LocalScale = entity.LocalScale with { Z = value };
                        }
                    }
                }

                ImGui.EndTable();
            }
            ImGui.PopStyleVar();

            static float LocalPositionXGetter(Entity entity) => entity.LocalPosition.X;
            static float LocalPositionYGetter(Entity entity) => entity.LocalPosition.Y;
            static float LocalPositionZGetter(Entity entity) => entity.LocalPosition.Z;

            static float LocalScaleXGetter(Entity entity) => entity.LocalScale.X;
            static float LocalScaleYGetter(Entity entity) => entity.LocalScale.Y;
            static float LocalScaleZGetter(Entity entity) => entity.LocalScale.Z;
        }
    }
}