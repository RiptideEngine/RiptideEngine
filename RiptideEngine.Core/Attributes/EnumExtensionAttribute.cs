namespace RiptideEngine.Core.Attributes;

[AttributeUsage(AttributeTargets.Enum)]
public sealed class EnumExtensionAttribute : Attribute {
    public string? ExtensionClassName { get; set; }

    public EnumExtensionAttribute() { }
}