namespace RiptideEditor;

// TODO: Shortcut rebinding.

internal static class Shortcuts {
    private static readonly List<ImGuiKey> _keys;
    private static readonly List<Shortcut> _shortcuts;

    private static Shortcut _executingShortcut;

    static Shortcuts() {
        _keys = [];
        _shortcuts = [];

        Type returnType = typeof(void);

        Span<Range> keySplit = stackalloc Range[2];

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            foreach (var type in assembly.GetTypes()) {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                    if (method.ReturnType != returnType) continue;
                    if (method.ContainsGenericParameters) continue;
                    if (method.GetParameters().Length != 0) continue;
                    if (method.GetCustomAttribute<ShortcutAttribute>() is not { } attribute) continue;
                    if (attribute.Keys.AsSpan().IsEmpty) continue;

                    if (!TryConvertKeys(attribute.Keys, out var keys)) continue;

                    _shortcuts.Add(new(attribute.Name, keys, attribute.ExecutionMode.IsDefined() ? attribute.ExecutionMode : ShortcutExecutionMode.Single, (Action)Delegate.CreateDelegate(typeof(Action), method)));
                }
            }
        }
    }

    private static bool TryConvertKeys(ReadOnlySpan<char> keys, out ImGuiKey[] outputKeys) {
        Span<Range> ranges = stackalloc Range[2];

        int length = GetKeysLength(keys);

        outputKeys = new ImGuiKey[length];
        int index = 0;
        var current = keys;

        while (true) {
            int split = current.Split(ranges, '+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ImGuiKey key;

            switch (split) {
                case 2:
                    if (!TryConvertKey(current[ranges[0]], out key)) return false;

                    outputKeys[index++] = key;
                    current = current[ranges[1]];
                    break;

                case 1:
                    if (!TryConvertKey(current[ranges[0]], out key)) return false;

                    outputKeys[index++] = key;
                    current = [];
                    break;

                case 0: goto breakout;
            }
        }

        breakout:
        Debug.Assert(index == length);

        return true;

        static int GetKeysLength(ReadOnlySpan<char> keys) {
            int length = 0;
            Span<Range> ranges = stackalloc Range[2];

            while (true) {
                int split = keys.Split(ranges, '+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                
                switch (split) {
                    case 2: length++; keys = keys[ranges[1]]; break;
                    case 1: length++; return length;
                    case 0: return length;
                    default: throw new UnreachableException();
                }
            }
        }

        static bool TryConvertKey(ReadOnlySpan<char> key, out ImGuiKey imguikey) {
            switch (key) {
                case "Tab": imguikey = ImGuiKey.Tab; return true;
                case "Left" or "LeftArrow" or "<-": imguikey = ImGuiKey.LeftArrow; return true;
                case "Right" or "RightArrow" or "->": imguikey = ImGuiKey.RightArrow; return true;
                case "Up" or "UpArrow": imguikey = ImGuiKey.UpArrow; return true;
                case "Down" or "DownArrow": imguikey = ImGuiKey.DownArrow; return true;
                case "Insert": imguikey = ImGuiKey.Insert; return true;
                case "Delete": imguikey = ImGuiKey.Delete; return true;
                case "Backspace": imguikey = ImGuiKey.Backspace; return true;
                case "Space": imguikey = ImGuiKey.Space; return true;
                case "Enter": imguikey = ImGuiKey.Enter; return true;
                case "Escape": imguikey = ImGuiKey.Escape; return true;
                case "Ctrl": imguikey = ImGuiKey.ModCtrl; return true;
                case "Shift": imguikey = ImGuiKey.ModShift; return true;
                case "Alt": imguikey = ImGuiKey.ModAlt; return true;
                case "0": imguikey = ImGuiKey._0; return true;
                case "1": imguikey = ImGuiKey._1; return true;
                case "2": imguikey = ImGuiKey._2; return true;
                case "3": imguikey = ImGuiKey._3; return true;
                case "4": imguikey = ImGuiKey._4; return true;
                case "5": imguikey = ImGuiKey._5; return true;
                case "6": imguikey = ImGuiKey._6; return true;
                case "7": imguikey = ImGuiKey._7; return true;
                case "8": imguikey = ImGuiKey._8; return true;
                case "9": imguikey = ImGuiKey._9; return true;
                case "A": imguikey = ImGuiKey.A; return true;
                case "B": imguikey = ImGuiKey.B; return true;
                case "C": imguikey = ImGuiKey.C; return true;
                case "D": imguikey = ImGuiKey.D; return true;
                case "E": imguikey = ImGuiKey.E; return true;
                case "F": imguikey = ImGuiKey.F; return true;
                case "G": imguikey = ImGuiKey.G; return true;
                case "H": imguikey = ImGuiKey.H; return true;
                case "I": imguikey = ImGuiKey.I; return true;
                case "J": imguikey = ImGuiKey.J; return true;
                case "K": imguikey = ImGuiKey.K; return true;
                case "L": imguikey = ImGuiKey.L; return true;
                case "M": imguikey = ImGuiKey.M; return true;
                case "N": imguikey = ImGuiKey.N; return true;
                case "O": imguikey = ImGuiKey.O; return true;
                case "P": imguikey = ImGuiKey.P; return true;
                case "Q": imguikey = ImGuiKey.Q; return true;
                case "R": imguikey = ImGuiKey.R; return true;
                case "S": imguikey = ImGuiKey.S; return true;
                case "T": imguikey = ImGuiKey.T; return true;
                case "U": imguikey = ImGuiKey.U; return true;
                case "V": imguikey = ImGuiKey.V; return true;
                case "W": imguikey = ImGuiKey.W; return true;
                case "X": imguikey = ImGuiKey.X; return true;
                case "Y": imguikey = ImGuiKey.Y; return true;
                case "Z": imguikey = ImGuiKey.Z; return true;
                case "F1": imguikey = ImGuiKey.F1; return true;
                case "F2": imguikey = ImGuiKey.F2; return true;
                case "F3": imguikey = ImGuiKey.F3; return true;
                case "F4": imguikey = ImGuiKey.F4; return true;
                case "F5": imguikey = ImGuiKey.F5; return true;
                case "F6": imguikey = ImGuiKey.F6; return true;
                case "F7": imguikey = ImGuiKey.F7; return true;
                case "F8": imguikey = ImGuiKey.F8; return true;
                case "F9": imguikey = ImGuiKey.F9; return true;
                case "F10": imguikey = ImGuiKey.F10; return true;
                case "F11": imguikey = ImGuiKey.F11; return true;
                case "F12": imguikey = ImGuiKey.F12; return true;
                case "'" or "Apostrophe": imguikey = ImGuiKey.Apostrophe; return true;
                case "-" or "Minus": imguikey = ImGuiKey.Minus; return true;
                case "." or "Period": imguikey = ImGuiKey.Period; return true;
                case "/" or "Slash" or "ForwardSlash": imguikey = ImGuiKey.Slash; return true;
                case ";" or "Semicolon": imguikey = ImGuiKey.Semicolon; return true;
                case "=" or "Equal": imguikey = ImGuiKey.Equal; return true;
                case "[" or "LeftBracket": imguikey = ImGuiKey.LeftBracket; return true;
                case "\\" or "Backslash": imguikey = ImGuiKey.Backslash; return true;
                case "]" or "RightBracket": imguikey = ImGuiKey.RightBracket; return true;
                case "`" or "GraveAccent": imguikey = ImGuiKey.GraveAccent; return true;

                default: imguikey = default; return false;
            }
        }
    }

    public static void Execute() {
        foreach (var it in Enumerable.Range((int)ImGuiKey.Tab, 15).Append((int)ImGuiKey.ModCtrl).Append((int)ImGuiKey.ModShift).Append((int)ImGuiKey.ModAlt).Concat(Enumerable.Range((int)ImGuiKey._0, ImGuiKey.GraveAccent - ImGuiKey._0 + 1))) {
            ImGuiKey key = (ImGuiKey)it;

            if (ImGui.IsKeyDown(key)) {
                if (_keys.Contains(key)) continue;

                _keys.Add(key);
            } else if (ImGui.IsKeyReleased(key)) {
                _keys.Remove(key);
            }
        }

        bool found = false;
        foreach (var shortcut in _shortcuts) {
            if (!shortcut.Keys.AsSpan().SequenceEqual(CollectionsMarshal.AsSpan(_keys))) continue;

            switch (shortcut.ExecutionMode) {
                case ShortcutExecutionMode.Single:
                    if (shortcut.Name == _executingShortcut.Name) break;

                    _executingShortcut = shortcut;
                    shortcut.Callback();
                    break;

                case ShortcutExecutionMode.Repeat:
                    _executingShortcut = shortcut;
                    shortcut.Callback();
                    break;
            }

            found = true;
            break;
        }

        if (!found) {
            _executingShortcut = default;
        }
    }

    private readonly record struct Shortcut(string Name, ImGuiKey[] Keys, ShortcutExecutionMode ExecutionMode, Action Callback);
}