namespace RiptideFoundation;

public interface IRenderingService : IRiptideService {
    BaseRenderingContext Context { get; }
}

internal class RenderingService : IRenderingService {
    public BaseRenderingContext Context { get; }

    public RenderingService(ContextOptions options) {
        Context = RenderingContext.CreateContext(options) ?? throw new ArgumentException("Failed to create rendering option.");
    }

    public void Dispose() {
        Context.Dispose();
    }
}