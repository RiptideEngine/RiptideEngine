namespace RiptideEditor;

public static unsafe class RiptideGUI {
    public static void DrawMatrixTable(ReadOnlySpan<char> stringID, Matrix4x4 matrix, ImGuiTableFlags flags) {
        if (ImGui.BeginTable(stringID, 5, flags)) {

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted("C1");
            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted("C2");
            ImGui.TableSetColumnIndex(3);
            ImGui.TextUnformatted("C3");
            ImGui.TableSetColumnIndex(4);
            ImGui.TextUnformatted("C4");

            Span<byte> rowText = stackalloc byte[3] {
                (byte)'R',
                0,
                0,
            };

            for (int r = 0; r < 4; r++) {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                rowText[1] = (byte)('1' + r);

                ImGuiNative.igTextUnformatted((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(rowText)), null);

                for (int c = 0; c < 4; c++) {
                    ImGui.TableSetColumnIndex(c + 1);
                    ImGui.TextUnformatted(matrix[r, c].ToString("F5"));
                }
            }

            ImGui.EndTable();
        }
    }

    public static bool NamedDragFloat2(int id, ref Vector2 vector, float speed, Vector2 min, Vector2 max, ReadOnlySpan<char> xName, ReadOnlySpan<char> yName, ReadOnlySpan<char> format, ImGuiSliderFlags flags) {
        bool changed = false;

        ImGui.PushID(id);
        {
            ImGuiInternal.PushMultiItemsWidths(2, ImGui.CalcItemWidth());

            var itemWidth = ImGui.CalcItemWidth();
            var oldCursorX = ImGui.GetCursorPosX();

            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted(xName); ImGui.SameLine();
            ImGui.SetNextItemWidth(itemWidth - (ImGui.GetCursorPosX() - oldCursorX));

            if (ImGui.DragFloat("##X", ref vector.X, speed, min.X, max.X, format, flags)) {
                changed = true;
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();

            itemWidth = ImGui.CalcItemWidth();
            oldCursorX = ImGui.GetCursorPosX();

            ImGui.AlignTextToFramePadding(); ImGui.TextUnformatted(yName); ImGui.SameLine();
            ImGui.SetNextItemWidth(itemWidth - (ImGui.GetCursorPosX() - oldCursorX));
            if (ImGui.DragFloat("##Y", ref vector.Y, speed, min.Y, max.Y, format, flags)) {
                changed = true;
            }
            ImGui.PopItemWidth();
        }
        ImGui.PopID();

        return changed;
    }

    private static class EnumMembers<T> where T : struct, Enum {
        public static readonly (string Name, T Value)[] Values = Enum.GetNames<T>().Zip(Enum.GetValues<T>()).ToArray();
    }

    public static bool EnumCombo<T>(ReadOnlySpan<char> id, ref T value, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, Enum {
        // Since T must be struct (value type), EqualityComparer<T> should devirtualize.

        string previewName = "<Undefined Value>";

        foreach ((var name, var val) in EnumMembers<T>.Values) {
            if (EqualityComparer<T>.Default.Equals(value, val)) {
                previewName = name;
                break;
            }
        }

        bool changed = false;

        if (ImGui.BeginCombo(id, previewName, flags)) {
            foreach ((var name, var val) in EnumMembers<T>.Values) {
                if (ImGui.Selectable(name, EqualityComparer<T>.Default.Equals(value, val))) {
                    value = val;
                    changed = true;
                }
            }

            ImGui.EndCombo();
        }

        return changed;
    }

    public static bool IsAnyMouseClicked() => ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right) || ImGui.IsMouseClicked(ImGuiMouseButton.Middle);

    public static bool IsMouseOverVoid() {
        return !ImGui.IsAnyItemHovered() && ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup);
    }
}