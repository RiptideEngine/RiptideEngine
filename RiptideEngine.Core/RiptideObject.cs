namespace RiptideEngine.Core;

public abstract class RiptideObject {
    public virtual string? Name { get; set; } = string.Empty;
}

public static class RiptideObjectExtension {
    public static T SetName<T>(this T obj, string? name) where T : RiptideObject {
        obj.Name = name;
        return obj;
    }
}