namespace RiptideEditor;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ComponentDrawerAttribute<T> : Attribute where T : Component { }

public abstract class BaseComponentDrawer {
    public Component TargetComponent { get; internal set; } = null!;

    public abstract void Render();
}