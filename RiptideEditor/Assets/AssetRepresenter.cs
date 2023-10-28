namespace RiptideEditor.Assets;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AssetRepresentationAttribute(string assetExtension) : Attribute {
    public string AssetExtension { get; private set; } = assetExtension;
}

public sealed class RepresentationContext {
}

public abstract class AssetRepresenter {
    public abstract void BuildRepresentation(RepresentationContext context);
}