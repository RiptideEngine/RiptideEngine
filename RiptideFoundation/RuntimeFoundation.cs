using RiptideRendering.Shadering;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace RiptideFoundation;

internal static unsafe class RuntimeFoundation {
    internal static IRenderingService RenderingService { get; private set; } = null!;
    internal static ISceneGraphService SceneGraphService { get; private set; } = null!;
    internal static IRuntimeWindowService WindowService { get; private set; } = null!;
    internal static IInputService InputService { get; private set; } = null!;
    internal static IComponentDatabase ComponentDatabase { get; private set; } = null!;
    internal static ResourceDatabase ResourceDatabase { get; private set; } = null!;

    internal static void AssertInitialized() {
        Debug.Assert(RenderingService != null, $"{nameof(RuntimeFoundation)} hasn't registered it's services to use yet. Have you called {nameof(RuntimeFoundation)}.{nameof(Initialize)}?");
    }

    public static void Initialize(RiptideServices services) {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        if (RenderingService != null) throw new InvalidOperationException("A service has already been used to initialize RiptideFoundation.");

        RenderingService = services.GetRequiredService<IRenderingService>();
        SceneGraphService = services.GetRequiredService<ISceneGraphService>();
        WindowService = services.GetRequiredService<IRuntimeWindowService>();
        InputService = services.GetRequiredService<IInputService>();
        ComponentDatabase = services.GetRequiredService<IComponentDatabase>();
        ResourceDatabase = (ResourceDatabase)services.GetRequiredService<IResourceDatabase>();
    }

    public static void Shutdown() { }
}