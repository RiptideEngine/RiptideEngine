namespace RiptideEditor;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AssetOpenAttribute(string assetExtension) : Attribute {
    public string AssetExtension { get; private set; } = assetExtension;
    public int Order { get; set; }
}