namespace RiptideEngine.SceneGraph;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EntityComponentAttribute : Attribute {
    public string Guid { get; private set; }

    public EntityComponentAttribute(string guid) {
        Guid = guid;
    }
}
