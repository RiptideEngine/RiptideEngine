namespace RiptideEditorV2.UI;

public readonly struct BatchingCandidate {
    public readonly MeshBuilder Builder;
    public readonly MaterialProperties Properties;

    internal BatchingCandidate(MeshBuilder builder, MaterialProperties properties) {
        Builder = builder;
        Properties = properties;
    }
}