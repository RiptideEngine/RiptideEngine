using RPCSToolkit.Attributes;

namespace RiptideEditor;

[EnumExtension]
public enum ShortcutExecutionMode {
    /// <summary>
    /// Shortcut only executes once when all keys are pressed.
    /// </summary>
    Single,

    /// <summary>
    /// Shortcut executes as long as all keys are being held.
    /// </summary>
    Repeat,
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ShortcutAttribute(string name, string keys) : Attribute {
    public string Name { get; private set; } = name;
    public string Keys { get; private set; } = keys;

    public ShortcutExecutionMode ExecutionMode { get; set; } = ShortcutExecutionMode.Single;
}