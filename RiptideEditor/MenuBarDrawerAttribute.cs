using RPCSToolkit.Attributes;

namespace RiptideEditor;

[EnumExtension]
public enum MenuBarSection {
    File,
    Edit,
    Resources,
    View,
    Tools,
    Help,
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MenuBarCallbackAttribute(MenuBarSection section, string path, string shortcut) : Attribute {
    public MenuBarSection Section { get; private set; } = section;
    public string Path { get; private set; } = path;
    public string Shortcut { get; private set; } = shortcut;

    public string? VisibilityMethod { get; set; }

    public MenuBarCallbackAttribute(MenuBarSection section, string path) : this(section, path, string.Empty) { }
}