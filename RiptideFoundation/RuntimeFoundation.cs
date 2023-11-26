namespace RiptideFoundation;

public static unsafe class RuntimeFoundation {
    internal static IInputService InputService { get; private set; } = null!;
    internal static ResourceDatabase ResourceDatabase { get; private set; } = null!;

    internal static void Initialize(RiptideServices services) {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        InputService = services.GetRequiredService<IInputService>();
        // ResourceDatabase = (ResourceDatabase)services.GetRequiredService<IResourceDatabase>();
    }

    internal static void Shutdown() { }
}