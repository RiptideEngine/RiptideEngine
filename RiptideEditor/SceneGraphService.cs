namespace RiptideEditor;

internal sealed class SceneGraphService : ISceneGraphService {
    public SceneContext Context { get; private set; } = new();

    public void Dispose() { }
}