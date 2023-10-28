using RiptideEngine.SceneGraph;

namespace Riptide;

internal static class Gameplay {
    public static readonly Scene<Node3D> Scene;

    static Gameplay() {
        Scene = new();
    }
}
