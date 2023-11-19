namespace RiptideFoundation;

public static unsafe class RuntimeFoundation {
    internal static IRenderingService RenderingService { get; private set; } = null!;
    internal static IInputService InputService { get; private set; } = null!;
    internal static ResourceDatabase ResourceDatabase { get; private set; } = null!;

    internal static void AssertInitialized() {
        Debug.Assert(RenderingService != null, $"{nameof(RuntimeFoundation)} hasn't registered it's services to use yet. Have you called {nameof(RuntimeFoundation)}.{nameof(Initialize)}?");
    }

    public static void Initialize(RiptideServices services) {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        if (RenderingService != null) throw new InvalidOperationException("A service has already been used to initialize RiptideFoundation.");

        RenderingService = services.GetRequiredService<IRenderingService>();
        InputService = services.GetRequiredService<IInputService>();
        // ResourceDatabase = (ResourceDatabase)services.GetRequiredService<IResourceDatabase>();
    }

    public static void Shutdown() { }
}