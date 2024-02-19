namespace RiptideFoundation;

public interface IRenderingService : IRiptideService {
    RenderingContext Context { get; }
}

public sealed class RenderingService(ContextOptions options) : IRenderingService {
    public RenderingContext Context { get; } = RenderingContext.CreateContext(options) ?? throw new ArgumentException("Failed to create rendering option.");

    public void Dispose() {
        Context.Dispose();
    }
}