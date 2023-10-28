namespace RiptideEngine.SceneGraph;

public static class EntityID {
    private static uint _id = 0;

    internal static uint Get() => Interlocked.Increment(ref _id);
    public static void Reset() => Interlocked.Exchange(ref _id, 0);
}