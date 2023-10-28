namespace RiptideEngine.SceneGraph;

public static class IterateOperation {
    /// <summary>
    /// Enumerate node downward, child by child.
    /// </summary>
    /// <param name="baseNode">Node to iterate from.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that iterate through every node.</returns>
    public static IEnumerable<Entity> IterateDownward(this Entity baseNode) {
        return EnumerateRecursive(baseNode);

        static IEnumerable<Entity> EnumerateRecursive(Entity node) {
            foreach (var child in node.EnumerateChildren()) {
                yield return child;

                foreach (var child2 in EnumerateRecursive(child)) yield return child2;
            }
        }
    }
}