using RiptideEngine.SceneGraph;

namespace Riptide;

public sealed unsafe class GameplayLayer {
    public GameplayLayer() {
        for (int i = 0; i < 10; i++) {
            var node = Gameplay.Scene.AddNode<Node3D>();

            node.LocalTransformation = Matrix4x4.CreateTranslation(i, i, 0);
            node.Name = "Node " + i;

            for (int j = 0; j < 5; j++) {
                var child = node.AddChild<Node3D>();

                child.LocalTransformation = Matrix4x4.CreateTranslation(0, 3, 0);
                child.Name = "Child " + j;
            }
        }
    }

    private Vector2 _cameraRot;
    public void Update() {
        
    }
}