namespace RiptideRendering;

public static unsafe class SilkHelper {
    public static string GetNativeName<T>(T value, string category) where T : Enum {
        var field = typeof(T).GetField(value.ToString());
        if (field == null) return string.Empty;

        var attributes = field.GetCustomAttributes<NativeNameAttribute>();

        foreach (var attribute in attributes) {
            if (attribute.Category == category) {
                return attribute.Name;
            }
        }

        return string.Empty;
    }

    public static string GetNativeName<T>(string category) {
        var attributes = typeof(T).GetCustomAttributes<NativeNameAttribute>();

        foreach (var attribute in attributes) {
            if (attribute.Category == category) {
                return attribute.Name;
            }
        }

        return string.Empty;
    }
}