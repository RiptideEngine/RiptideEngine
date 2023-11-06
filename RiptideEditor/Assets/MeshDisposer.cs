namespace RiptideEditor.Assets;

internal sealed class MeshDisposer : ResourceDisposer {
    public override bool TryDispose(object resource) {
        if (resource is Mesh mesh) {
            mesh.DecrementReference();
            return true;
        }

        return false;
    }
}