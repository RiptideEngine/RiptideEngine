namespace RiptideFoundation;

public static class Graphics {
    internal static RenderingContext RenderingContext { get; private set; } = null!;
    internal static RenderingPipeline RenderingPipeline { get; set; } = null!;

    internal static void Initialize(RiptideServices services) {
        RenderingContext = services.GetRequiredService<IRenderingService>().Context;
    }
}