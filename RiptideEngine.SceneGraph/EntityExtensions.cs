namespace RiptideEngine.SceneGraph;

public static class EntityExtensions {
    public static bool IsDescendantOf(this Entity entity, Entity parent) {
        var currParent = entity.Parent;

        while (currParent != null) {
            if (currParent == parent) return true;

            currParent = currParent.Parent;
        }

        return false;
    }
}