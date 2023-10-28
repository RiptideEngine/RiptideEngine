namespace RiptideEditor;

internal sealed class SceneGraphService : ISceneGraphService {
    public SceneContext EditorContext { get; private set; }
    public SceneContext PreviewContext { get; private set; }

    public SceneContext Context { get; private set; }

    public SceneGraphService() {
        EditorContext = new SceneContext();
        PreviewContext = new SceneContext();
        Context = new();
    }

    public void Dispose() { }
}