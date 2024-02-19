using RiptideFoundation.Rendering;

namespace RiptideEditorV2.UI;

public readonly struct BatchedCandidate {
    public readonly Mesh Mesh;
    public readonly MaterialProperties Properties;

    internal BatchedCandidate(Mesh mesh, MaterialProperties properties) {
        Mesh = mesh;
        Properties = properties;
    }
}